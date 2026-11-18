using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using TMPro;

public class BattleSystem : MonoBehaviour
{
    private sealed class EnemyState
    {
        public EnemyDefinition Definition;
        public int HP;
    }

    private sealed class EnemyUI
    {
        public GameObject root;
        public Image battleSprite;
        public TextMeshProUGUI nameText;
        public TextMeshProUGUI hpText;
        public Image hpFill;
    }

    private sealed class PartyMemberUI
    {
        public GameObject root;
        public Image background;
        public Outline outline;
        public TextMeshProUGUI turnIndicator;
        public TextMeshProUGUI nameText;
        public TextMeshProUGUI statsText;
        public TextMeshProUGUI hpText;
        public TextMeshProUGUI mpText;
        public Image hpFill;
        public Image mpFill;
        public Image battleSprite;
        public RectTransform battleSpriteRoot;
        public Vector2 battleSpriteTarget;
    }

    [SerializeField] private GameState gameState;
    [SerializeField] private float actionDelay = 0.6f;
    [SerializeField, Min(0.1f)] private float transitionDuration = 0.8f;
    [SerializeField] private Sprite defaultBattleRoomBackground;
    [SerializeField] private TMP_FontAsset battleOptionsFont;
    [SerializeField] private TMP_FontAsset statusFont;

    readonly List<EnemyState> enemies = new List<EnemyState>();
    readonly List<EnemyUI> enemyRows = new List<EnemyUI>();
    readonly List<PartyMemberUI> partyRows = new List<PartyMemberUI>();
    readonly List<Button> spellButtons = new List<Button>();
    readonly List<Image> spellBackgrounds = new List<Image>();
    readonly List<TextMeshProUGUI> spellIndicators = new List<TextMeshProUGUI>();
    readonly List<Button> targetButtons = new List<Button>();
    readonly List<Image> targetBackgrounds = new List<Image>();
    readonly List<TextMeshProUGUI> targetIndicators = new List<TextMeshProUGUI>();
    Canvas battleCanvas;
    GameObject battlePanel;
    Image roomBackgroundImage;
    Image transitionCurtain;
    Transform enemyListRoot;
    Transform partyStatusRoot;
    GameObject spellMenuRoot;
    TextMeshProUGUI spellMenuTitle;
    GameObject targetMenuRoot;
    TextMeshProUGUI targetMenuTitle;
    TextMeshProUGUI controlsText;
    TextMeshProUGUI messageText;
    readonly Button[] actionButtons = new Button[5];
    readonly Image[] actionBackgrounds = new Image[5];
    readonly TextMeshProUGUI[] actionIndicators = new TextMeshProUGUI[5];
    int selectedActionIndex;
    bool isBattleActive;
    bool isResolvingTurn;
    bool isTransitioning;
    bool isSpellMenuOpen;
    bool isTargetMenuOpen;
    bool returnToSpellMenuAfterTarget;
    bool defending;
    int currentMemberIndex;
    int actionsThisRound;
    int selectedSpellIndex;
    int selectedTargetIndex;
    System.Action<EnemyState> pendingEnemyTargetAction;

    private static readonly string[] ActionLabels = { "Attack", "Skill", "Item", "Defend", "Flee" };

    public event System.Action BattleClosed;

    public bool IsBattleActive => isBattleActive || isTransitioning || (battlePanel != null && battlePanel.activeSelf);

    private void Awake()
    {
        if (gameState == null)
            gameState = GameState.Instance;
        BuildUI();
        battlePanel.SetActive(false);
    }

    private void Update()
    {
        if (!isBattleActive || isResolvingTurn || isTransitioning)
            return;

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
            return;
        if (isTargetMenuOpen)
        {
            if (keyboard.escapeKey.wasPressedThisFrame || keyboard.backspaceKey.wasPressedThisFrame)
                CloseEnemyTargetMenu(true);
            else HandleChoiceInput(keyboard, targetButtons, ref selectedTargetIndex, RefreshTargetSelection);
            return;
        }
        if (isSpellMenuOpen)
        {
            if (keyboard.escapeKey.wasPressedThisFrame || keyboard.backspaceKey.wasPressedThisFrame)
                CloseSpellMenu();
            else HandleChoiceInput(keyboard, spellButtons, ref selectedSpellIndex, RefreshSpellSelection);
            return;
        }
        if (IsChoiceInput(keyboard))
            HandleChoiceInput(keyboard, actionButtons, ref selectedActionIndex, RefreshActionSelection);
        else if (keyboard.digit1Key.wasPressedThisFrame) Attack();
        else if (keyboard.digit2Key.wasPressedThisFrame) UseSkill();
        else if (keyboard.digit3Key.wasPressedThisFrame) UseItem();
        else if (keyboard.digit4Key.wasPressedThisFrame) Defend();
        else if (keyboard.digit5Key.wasPressedThisFrame) Flee();
    }

    public bool StartBattle(EnemyDefinition[] encounterEnemies)
    {
        return StartBattle(encounterEnemies, null);
    }

    public bool StartBattle(EnemyDefinition[] encounterEnemies, Sprite battleRoomBackground)
    {
        if (isBattleActive || isTransitioning || (battlePanel != null && battlePanel.activeSelf))
            return false;
        if (gameState == null)
            gameState = GameState.Instance;
        if (gameState == null || encounterEnemies == null || encounterEnemies.Length == 0)
        {
            Debug.LogError("A GameState and at least one enemy are required to start a battle.", this);
            return false;
        }

        enemies.Clear();
        foreach (EnemyDefinition definition in encounterEnemies)
        {
            if (definition != null)
                enemies.Add(new EnemyState { Definition = definition, HP = definition.MaxHP });
        }
        bool hasLivingParty = false;
        foreach (PartyMemberState member in gameState.Party)
            hasLivingParty |= member != null && member.IsAlive;
        if (enemies.Count == 0 || !hasLivingParty)
        {
            Debug.LogWarning("Battle could not start because there are no valid enemies or living party members.", this);
            return false;
        }

        isBattleActive = true;
        isResolvingTurn = false;
        isTransitioning = true;
        defending = false;
        currentMemberIndex = 0;
        actionsThisRound = 0;
        selectedActionIndex = 0;
        BuildUI();
        BuildPartyRows();
        BuildEnemyRows();
        SetBattleRoomBackground(battleRoomBackground != null ? battleRoomBackground : defaultBattleRoomBackground);
        SetMessage("Enemies approach! Select a command.");
        RefreshUI();
        StartCoroutine(TransitionIntoBattle());
        return true;
    }

    public void Attack()
    {
        if (!CanAct()) return;
        if (enemies.Find(enemy => enemy.HP > 0) == null)
            return;

        OpenEnemyTargetMenu(ResolveAttack, false);
    }

    private void ResolveAttack(EnemyState target)
    {
        if (!CanAct() || target == null || target.HP <= 0) return;
        PartyMemberState attacker = GetNextLivingMember();
        if (attacker == null) return;

        int damage = Mathf.Max(1, attacker.AttackPower - target.Definition.Defense);
        target.HP = Mathf.Max(0, target.HP - damage);
        SetMessage(attacker.Character.DisplayName + " attacks " + target.Definition.DisplayName + " for " + damage + " damage.");
        RefreshUI();
        if (AllEnemiesDefeated())
        {
            FinishBattle(true);
            return;
        }
        CompletePartyAction();
    }

    public void Defend()
    {
        if (!CanAct()) return;
        defending = true;
        PartyMemberState member = GetNextLivingMember();
        SetMessage((member != null ? member.Character.DisplayName : "The party") + " braces for the next attack.");
        CompletePartyAction();
    }

    public void UseItem()
    {
        if (!CanAct()) return;
        ItemDefinition usableItem = FindUsefulItem(out PartyMemberState target);
        if (target == null || usableItem == null || !gameState.TryUseItem(usableItem, FindPartyMemberIndex(target)))
        {
            SetMessage("There is no usable healing item.");
            RefreshUI();
            return;
        }

        GetNextLivingMember();
        SetMessage(target.Character.DisplayName + " uses " + usableItem.DisplayName + ".");
        CompletePartyAction();
    }

    public void UseSkill()
    {
        if (!CanAct()) return;
        PartyMemberState user = GetCurrentLivingMember();
        if (user == null) return;
        List<SkillDefinition> skills = new List<SkillDefinition>();
        foreach (SkillDefinition candidate in user.Character.Skills)
        {
            if (candidate != null)
                skills.Add(candidate);
        }
        if (skills.Count == 0)
        {
            SetMessage(user.Character.DisplayName + " has no spells.");
            RefreshUI();
            return;
        }

        OpenSpellMenu(user, skills);
    }

    private void CastSkill(SkillDefinition skill, EnemyState damageTarget = null)
    {
        if (!CanAct() || skill == null) return;
        PartyMemberState user = GetCurrentLivingMember();
        if (user == null || user.CurrentMP < skill.MPCost)
        {
            SetMessage(user != null ? user.Character.DisplayName + " does not have enough MP." : "No party member can cast a spell.");
            CloseSpellMenu();
            RefreshUI();
            return;
        }

        PartyMemberState healingTarget = null;
        if (skill.Effect == SkillEffect.DamageEnemy)
        {
            if (damageTarget == null || damageTarget.HP <= 0)
            {
                SetMessage("That enemy is no longer a valid target.");
                CloseSpellMenu();
                RefreshUI();
                return;
            }
        }
        else
        {
            healingTarget = FindMostInjuredMember();
            if (healingTarget == null)
            {
                SetMessage("No party member needs healing.");
                CloseSpellMenu();
                RefreshUI();
                return;
            }
        }
        CloseSpellMenu();
        GetNextLivingMember();
        if (damageTarget != null)
        {
            damageTarget.HP = Mathf.Max(0, damageTarget.HP - skill.Power);
            SetMessage(user.Character.DisplayName + " uses " + skill.DisplayName + " for " + skill.Power + " damage.");
        }
        else
        {
            int restored = Mathf.Min(skill.Power, healingTarget.MaxHP - healingTarget.CurrentHP);
            healingTarget.CurrentHP += restored;
            SetMessage(user.Character.DisplayName + " uses " + skill.DisplayName + " to restore " + restored + " HP.");
        }
        user.CurrentMP -= skill.MPCost;
        RefreshUI();
        if (AllEnemiesDefeated())
        {
            FinishBattle(true);
            return;
        }
        CompletePartyAction();
    }

    public void Flee()
    {
        if (!CanAct()) return;
        if (Random.value < 0.5f)
        {
            isBattleActive = false;
            SetButtons(false);
            SetMessage("The party escaped.");
            StartCoroutine(CloseBattleAfterDelay());
            return;
        }

        SetMessage("The escape attempt failed!");
        GetNextLivingMember();
        CompletePartyAction();
    }

    private IEnumerator EnemyTurn()
    {
        isResolvingTurn = true;
        SetButtons(false);
        yield return new WaitForSecondsRealtime(Mathf.Max(0f, actionDelay));

        List<EnemyState> livingEnemies = enemies.FindAll(enemy => enemy.HP > 0);
        foreach (EnemyState enemy in livingEnemies)
        {
            PartyMemberState target = FindRandomLivingMember();
            if (target == null)
            {
                FinishBattle(false);
                yield break;
            }

            int rawDamage = Mathf.Max(1, enemy.Definition.Attack - target.DefensePower);
            int damage = defending ? Mathf.Max(1, rawDamage / 2) : rawDamage;
            target.CurrentHP = Mathf.Max(0, target.CurrentHP - damage);
            SetMessage(enemy.Definition.DisplayName + " hits " + target.Character.DisplayName + " for " + damage + " damage.");
            RefreshUI();
            yield return new WaitForSecondsRealtime(Mathf.Max(0f, actionDelay));
        }

        defending = false;
        isResolvingTurn = false;
        currentMemberIndex = FindNextLivingMemberIndex(currentMemberIndex);
        actionsThisRound = 0;
        if (AllPartyDefeated())
        {
            FinishBattle(false);
            yield break;
        }

        SetMessage("Choose the next party action.");
        RefreshUI();
    }

    private void FinishBattle(bool victory)
    {
        isResolvingTurn = false;
        isBattleActive = false;
        SetButtons(false);
        if (victory)
        {
            int experience = 0;
            int gold = 0;
            foreach (EnemyState enemy in enemies)
            {
                experience += enemy.Definition.ExperienceReward;
                gold += enemy.Definition.GoldReward;
                foreach (ItemDefinition drop in enemy.Definition.PossibleDrops)
                {
                    if (drop != null && Random.value < 0.25f)
                        gameState.AddItem(drop);
                }
            }
            gameState.AwardExperience(experience);
            gameState.AddGold(gold);
            SetMessage("Victory! Gained " + experience + " EXP and " + gold + " gold.");
        }
        else
        {
            SetMessage("The party has been defeated.");
        }
        RefreshUI();
        StartCoroutine(CloseBattleAfterDelay());
    }

    private IEnumerator CloseBattleAfterDelay()
    {
        yield return new WaitForSecondsRealtime(2.5f);
        if (battlePanel != null && !isBattleActive)
            yield return TransitionOutOfBattle();
    }

    private bool CanAct()
    {
        return isBattleActive && !isResolvingTurn && !isTransitioning;
    }

    private PartyMemberState GetNextLivingMember()
    {
        PartyMemberState member = GetCurrentLivingMember();
        if (member != null)
            currentMemberIndex = (currentMemberIndex + 1) % gameState.Party.Count;
        return member;
    }

    private PartyMemberState GetCurrentLivingMember()
    {
        for (int i = 0; i < gameState.Party.Count; i++)
        {
            int index = (currentMemberIndex + i) % gameState.Party.Count;
            PartyMemberState member = gameState.Party[index];
            if (member != null && member.IsAlive)
            {
                currentMemberIndex = index;
                return member;
            }
        }
        return null;
    }

    private void CompletePartyAction()
    {
        actionsThisRound++;
        if (actionsThisRound >= GetLivingPartyCount())
        {
            StartCoroutine(EnemyTurn());
            return;
        }

        PartyMemberState next = GetCurrentLivingMember();
        SetMessage(next != null ? next.Character.DisplayName + "'s turn. Choose an action." : "Choose the next party action.");
        RefreshUI();
    }

    private int GetLivingPartyCount()
    {
        int count = 0;
        foreach (PartyMemberState member in gameState.Party)
        {
            if (member != null && member.IsAlive)
                count++;
        }
        return count;
    }

    private int FindNextLivingMemberIndex(int startIndex)
    {
        if (gameState.Party.Count == 0) return 0;
        for (int i = 0; i < gameState.Party.Count; i++)
        {
            int index = (startIndex + i) % gameState.Party.Count;
            PartyMemberState member = gameState.Party[index];
            if (member != null && member.IsAlive)
                return index;
        }
        return 0;
    }

    private PartyMemberState FindRandomLivingMember()
    {
        List<PartyMemberState> living = new List<PartyMemberState>();
        foreach (PartyMemberState member in gameState.Party)
        {
            if (member != null && member.IsAlive)
                living.Add(member);
        }
        return living.Count == 0 ? null : living[Random.Range(0, living.Count)];
    }

    private PartyMemberState FindMostInjuredMember()
    {
        PartyMemberState result = null;
        float lowestHealthRatio = 1f;
        foreach (PartyMemberState member in gameState.Party)
        {
            if (member == null || !member.IsAlive || member.CurrentHP >= member.MaxHP)
                continue;
            float ratio = (float)member.CurrentHP / member.MaxHP;
            if (ratio < lowestHealthRatio)
            {
                lowestHealthRatio = ratio;
                result = member;
            }
        }
        return result;
    }

    private int FindPartyMemberIndex(PartyMemberState target)
    {
        for (int i = 0; i < gameState.Party.Count; i++)
        {
            if (gameState.Party[i] == target)
                return i;
        }
        return -1;
    }

    private ItemDefinition FindUsefulItem(out PartyMemberState target)
    {
        target = null;
        foreach (InventoryStack stack in gameState.Inventory)
        {
            if (stack == null || stack.Item == null || stack.Quantity <= 0) continue;
            if (stack.Item.Category != ItemCategory.Consumable) continue;
            foreach (PartyMemberState member in gameState.Party)
            {
                if (member == null || !member.IsAlive) continue;
                bool needsItem = stack.Item.Effect == ItemEffect.HealHP && member.CurrentHP < member.MaxHP ||
                                 stack.Item.Effect == ItemEffect.RestoreMP && member.CurrentMP < member.MaxMP;
                if (!needsItem) continue;
                target = member;
                return stack.Item;
            }
        }
        return null;
    }

    private bool AllEnemiesDefeated()
    {
        return enemies.TrueForAll(enemy => enemy.HP <= 0);
    }

    private bool AllPartyDefeated()
    {
        foreach (PartyMemberState member in gameState.Party)
        {
            if (member != null && member.IsAlive)
                return false;
        }
        return true;
    }

    private void BuildUI()
    {
        if (battleCanvas == null)
        {
            GameObject canvasObject = new GameObject("Battle Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            DontDestroyOnLoad(canvasObject);
            battleCanvas = canvasObject.GetComponent<Canvas>();
            battleCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            if (FindAnyObjectByType<EventSystem>() == null)
            {
                GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem));
                eventSystem.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();
            }
        }
        if (battlePanel != null) return;

        battlePanel = new GameObject("Battle HUD", typeof(RectTransform), typeof(Image));
        battlePanel.transform.SetParent(battleCanvas.transform, false);
        RectTransform panelRect = battlePanel.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;
        battlePanel.GetComponent<Image>().color = Color.clear;

        GameObject room = CreateRect("Battle Room", battlePanel.transform, Vector2.zero, Vector2.one, new Color(0.12f, 0.16f, 0.21f));
        roomBackgroundImage = room.GetComponent<Image>();
        CreateRect("Battle Room Shade", battlePanel.transform, Vector2.zero, Vector2.one, new Color(0.015f, 0.025f, 0.055f, 0.18f));
        CreateRect("Arena Floor", battlePanel.transform, new Vector2(0f, 0f), new Vector2(1f, 0.32f), new Color(0.27f, 0.16f, 0.11f, 0.94f));
        CreateRect("Arena Horizon", battlePanel.transform, new Vector2(0f, 0.315f), new Vector2(1f, 0.325f), new Color(0.8f, 0.58f, 0.34f, 0.75f));
        enemyListRoot = CreateRect("Enemy Formation", battlePanel.transform, Vector2.zero, Vector2.one, Color.clear).transform;
        partyStatusRoot = CreateRect("Party Status Strip", battlePanel.transform, new Vector2(0.46f, 0.018f), new Vector2(0.99f, 0.19f), Color.clear).transform;

        CreateCard("Command Menu", battlePanel.transform, new Vector2(0.72f, 0.43f), new Vector2(0.985f, 0.94f));
        CreateText("COMMAND", battlePanel.transform, new Vector2(0.735f, 0.875f), new Vector2(0.97f, 0.92f), 18, TextAnchor.MiddleLeft, AccentColor, FontStyle.Bold);
        UnityEngine.Events.UnityAction[] actions = { Attack, UseSkill, UseItem, Defend, Flee };
        for (int i = 0; i < actionButtons.Length; i++)
        {
            float top = 0.865f - i * 0.079f;
            float bottom = top - 0.07f;
            CreateActionButton(i, new Vector2(0.73f, bottom), new Vector2(0.975f, top), actions[i]);
        }

        CreateCard("Battle Message", battlePanel.transform, new Vector2(0.02f, 0.02f), new Vector2(0.45f, 0.145f));
        messageText = CreateText("Battle Message Text", battlePanel.transform, new Vector2(0.035f, 0.035f), new Vector2(0.435f, 0.13f), 18, TextAnchor.MiddleLeft, Color.white, FontStyle.Normal);
        controlsText = CreateText("Controls", battlePanel.transform, new Vector2(0.025f, 0.15f), new Vector2(0.42f, 0.19f), 13, TextAnchor.MiddleLeft, Color.white, FontStyle.Normal);
        controlsText.text = "↑ / ↓ Select     Enter Confirm";

        BuildPartyRows();
        BuildEnemyRows();
        spellMenuRoot = BuildChoiceMenu("Spell Menu", "Spell Menu Title", out spellMenuTitle);
        targetMenuRoot = BuildChoiceMenu("Enemy Target Menu", "Enemy Target Menu Title", out targetMenuTitle);

        GameObject curtainObject = new GameObject("Battle Transition Curtain", typeof(RectTransform), typeof(Image));
        curtainObject.transform.SetParent(battleCanvas.transform, false);
        RectTransform curtainRect = curtainObject.GetComponent<RectTransform>();
        curtainRect.anchorMin = Vector2.zero;
        curtainRect.anchorMax = Vector2.one;
        curtainRect.offsetMin = Vector2.zero;
        curtainRect.offsetMax = Vector2.zero;
        transitionCurtain = curtainObject.GetComponent<Image>();
        transitionCurtain.color = new Color(0f, 0f, 0f, 0f);
        transitionCurtain.raycastTarget = true;
        curtainObject.SetActive(false);
    }

    private static readonly Color AccentColor = new Color(0.48f, 0.82f, 1f);
    private static readonly Color MutedColor = new Color(0.56f, 0.67f, 0.79f);
    private static readonly Color FrameColor = new Color(0.22f, 0.48f, 0.72f, 0.98f);
    private static readonly Color PanelColor = new Color(0.025f, 0.07f, 0.15f, 0.97f);

    private RectTransform CreateStageSprite(string objectName, Transform parent, Vector2 anchor, Vector2 size, Sprite sprite, out Image image)
    {
        GameObject actor = new GameObject(objectName, typeof(RectTransform), typeof(Image), typeof(Outline));
        actor.transform.SetParent(parent, false);
        RectTransform rect = actor.GetComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.sizeDelta = size;
        rect.anchoredPosition = Vector2.zero;

        image = actor.GetComponent<Image>();
        image.sprite = sprite;
        image.preserveAspect = sprite != null;
        image.color = Color.white;
        image.enabled = sprite != null;
        image.raycastTarget = false;
        Outline outline = actor.GetComponent<Outline>();
        outline.effectColor = new Color(0.64f, 0.81f, 0.95f, 0.8f);
        outline.effectDistance = new Vector2(2f, 2f);
        outline.enabled = sprite != null;
        return rect;
    }

    private void SetBattleRoomBackground(Sprite background)
    {
        if (roomBackgroundImage == null)
            return;

        roomBackgroundImage.sprite = background;
        roomBackgroundImage.preserveAspect = false;
        roomBackgroundImage.color = background != null ? Color.white : new Color(0.18f, 0.2f, 0.22f, 1f);
    }

    private IEnumerator TransitionIntoBattle()
    {
        transitionCurtain.gameObject.SetActive(true);
        yield return FadeTransitionCurtain(0f, 1f, transitionDuration * 0.5f, false);
        battlePanel.SetActive(true);
        SetStageActorsPosition(0f);
        yield return FadeTransitionCurtain(1f, 0f, transitionDuration * 0.5f, true);
        transitionCurtain.gameObject.SetActive(false);
        isTransitioning = false;
        RefreshUI();
    }

    private IEnumerator TransitionOutOfBattle()
    {
        isTransitioning = true;
        transitionCurtain.gameObject.SetActive(true);
        transitionCurtain.color = new Color(0f, 0f, 0f, 0f);
        yield return FadeTransitionCurtain(0f, 1f, transitionDuration * 0.5f, false);
        battlePanel.SetActive(false);
        yield return FadeTransitionCurtain(1f, 0f, transitionDuration * 0.5f, false);
        transitionCurtain.gameObject.SetActive(false);
        isTransitioning = false;
        BattleClosed?.Invoke();
    }

    private IEnumerator FadeTransitionCurtain(float from, float to, float duration, bool slideActors)
    {
        float elapsed = 0f;
        float safeDuration = Mathf.Max(0.01f, duration);
        while (elapsed < safeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / safeDuration);
            float easedProgress = progress * progress * (3f - 2f * progress);
            transitionCurtain.color = new Color(0f, 0f, 0f, Mathf.Lerp(from, to, easedProgress));
            if (slideActors)
                SetStageActorsPosition(easedProgress);
            yield return null;
        }
        transitionCurtain.color = new Color(0f, 0f, 0f, to);
        if (slideActors)
            SetStageActorsPosition(1f);
    }

    private void SetStageActorsPosition(float progress)
    {
        foreach (EnemyUI enemy in enemyRows)
        {
            enemy.battleSprite.rectTransform.anchoredPosition = Vector2.Lerp(Vector2.left * 640f, Vector2.zero, progress);
        }
        foreach (PartyMemberUI member in partyRows)
        {
            if (member.battleSpriteRoot == null)
                continue;
            Vector2 start = member.battleSpriteTarget + Vector2.right * 640f;
            member.battleSpriteRoot.anchoredPosition = Vector2.Lerp(start, member.battleSpriteTarget, progress);
        }
    }

    private GameObject CreateCard(string name, Transform parent, Vector2 min, Vector2 max)
    {
        GameObject frame = CreateRect(name, parent, min, max, FrameColor);
        AddOutline(frame, new Color(0.5f, 0.78f, 0.98f, 1f), 2f);
        GameObject inside = CreateRect(name + " Inner", frame.transform, Vector2.zero, Vector2.one, PanelColor);
        RectTransform innerRect = inside.GetComponent<RectTransform>();
        innerRect.offsetMin = new Vector2(3f, 3f);
        innerRect.offsetMax = new Vector2(-3f, -3f);
        return inside;
    }

    private GameObject CreateRect(string name, Transform parent, Vector2 min, Vector2 max, Color color)
    {
        GameObject instance = new GameObject(name, typeof(RectTransform), typeof(Image));
        instance.transform.SetParent(parent, false);
        RectTransform rect = instance.GetComponent<RectTransform>();
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        Image image = instance.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return instance;
    }

    private void AddOutline(GameObject instance, Color color, float thickness)
    {
        Outline outline = instance.AddComponent<Outline>();
        outline.effectColor = color;
        outline.effectDistance = new Vector2(thickness, thickness);
    }

    private TextMeshProUGUI CreateText(string objectName, Transform parent, Vector2 min, Vector2 max, int size, TextAnchor alignment, Color color, FontStyle style, TMP_FontAsset font = null)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);
        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = new Vector2(8, 2);
        rect.offsetMax = new Vector2(-8, -2);
        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.font = font != null ? font : TMP_Settings.defaultFontAsset;
        text.fontSize = size;
        text.fontStyle = style == FontStyle.Bold ? FontStyles.Bold : FontStyles.Normal;
        text.color = color;
        text.alignment = alignment == TextAnchor.MiddleCenter ? TextAlignmentOptions.Midline
            : alignment == TextAnchor.MiddleRight ? TextAlignmentOptions.MidlineRight
            : TextAlignmentOptions.MidlineLeft;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Overflow;
        text.raycastTarget = false;
        return text;
    }

    private void CreateActionButton(int index, Vector2 min, Vector2 max, UnityEngine.Events.UnityAction action)
    {
        GameObject buttonObject = new GameObject(ActionLabels[index], typeof(RectTransform), typeof(Image), typeof(Button), typeof(Outline));
        buttonObject.transform.SetParent(battlePanel.transform, false);
        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        Image background = buttonObject.GetComponent<Image>();
        background.color = PanelColor;
        AddOutline(buttonObject, new Color(0.25f, 0.52f, 0.75f, 0.55f), 1f);

        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = background;
        button.transition = Selectable.Transition.None;
        button.onClick.AddListener(action);
        actionButtons[index] = button;
        actionBackgrounds[index] = background;
        actionIndicators[index] = CreateText("Selection Arrow", buttonObject.transform, new Vector2(0.02f, 0f), new Vector2(0.22f, 1f), 20, TextAnchor.MiddleCenter, AccentColor, FontStyle.Bold);
        actionIndicators[index].text = string.Empty;
        TextMeshProUGUI label = CreateText("Action Label", buttonObject.transform, new Vector2(0.22f, 0f), new Vector2(0.76f, 1f), 17, TextAnchor.MiddleLeft, Color.white, FontStyle.Normal, battleOptionsFont);
        label.text = ActionLabels[index];
        TextMeshProUGUI shortcut = CreateText("Shortcut", buttonObject.transform, new Vector2(0.76f, 0f), new Vector2(0.98f, 1f), 14, TextAnchor.MiddleRight, MutedColor, FontStyle.Normal, battleOptionsFont);
        shortcut.text = (index + 1).ToString();
    }

    private GameObject BuildChoiceMenu(string name, string title, out TextMeshProUGUI titleText)
    {
        GameObject menu = CreateRect(name, battlePanel.transform, new Vector2(0.72f, 0.43f), new Vector2(0.985f, 0.94f), PanelColor);
        AddOutline(menu, new Color(0.5f, 0.78f, 0.98f, 1f), 2f);
        titleText = CreateText(title, menu.transform, new Vector2(0.04f, 0.84f), new Vector2(0.96f, 0.98f), 17, TextAnchor.MiddleLeft, AccentColor, FontStyle.Bold);
        menu.SetActive(false);
        return menu;
    }

    private void OpenEnemyTargetMenu(System.Action<EnemyState> onTargetSelected, bool returnToSpellMenu)
    {
        List<EnemyState> livingEnemies = enemies.FindAll(enemy => enemy.HP > 0);
        if (livingEnemies.Count == 0)
        {
            SetMessage("There are no enemies left to target.");
            return;
        }

        pendingEnemyTargetAction = onTargetSelected;
        returnToSpellMenuAfterTarget = returnToSpellMenu;
        isTargetMenuOpen = true;
        isSpellMenuOpen = false;
        controlsText.text = "↑ / ↓ Target     Enter Confirm     Esc Back";
        spellMenuRoot.SetActive(false);
        targetMenuTitle.text = "Select an enemy";
        ClearChoiceItems(targetMenuRoot.transform, targetMenuTitle, targetButtons, targetBackgrounds, targetIndicators);
        for (int i = 0; i < livingEnemies.Count; i++)
        {
            EnemyState target = livingEnemies[i];
            CreateTargetButton(target.Definition.DisplayName + "  (" + target.HP + " HP)", i, livingEnemies.Count + 1, () => SelectEnemyTarget(target));
        }
        CreateTargetButton("Back", livingEnemies.Count, livingEnemies.Count + 1, () => CloseEnemyTargetMenu(true));
        selectedTargetIndex = 0;
        targetMenuRoot.SetActive(true);
        SetButtons(CanAct());
        RefreshTargetSelection();
    }

    private void CreateTargetButton(string label, int index, int rowCount, UnityEngine.Events.UnityAction action)
    {
        CreateChoiceButton(targetMenuRoot.transform, label, index, rowCount, true, action, targetButtons, targetBackgrounds, targetIndicators, "Target Name");
    }

    private GameObject CreateChoiceButton(Transform parent, string label, int index, int rowCount, bool interactable,
        UnityEngine.Events.UnityAction action, List<Button> buttons, List<Image> backgrounds, List<TextMeshProUGUI> indicators, string labelName)
    {
        GameObject item = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button), typeof(Outline));
        item.transform.SetParent(parent, false);
        RectTransform rect = item.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.04f, 0.82f - 0.78f * (index + 1) / rowCount);
        rect.anchorMax = new Vector2(0.96f, 0.82f - 0.78f * index / rowCount);
        rect.offsetMin = new Vector2(0f, 2f);
        rect.offsetMax = new Vector2(0f, -2f);

        Image background = item.GetComponent<Image>();
        background.color = PanelColor;
        Button button = item.GetComponent<Button>();
        button.targetGraphic = background;
        button.transition = Selectable.Transition.None;
        button.interactable = interactable;
        button.onClick.AddListener(action);
        buttons.Add(button);
        backgrounds.Add(background);
        indicators.Add(CreateText("Selection Arrow", item.transform, new Vector2(0.01f, 0f), new Vector2(0.2f, 1f), 18, TextAnchor.MiddleCenter, AccentColor, FontStyle.Bold));
        CreateText(labelName, item.transform, new Vector2(0.2f, 0f), new Vector2(0.96f, 1f), 15, TextAnchor.MiddleLeft, Color.white, FontStyle.Normal, battleOptionsFont).text = label;
        return item;
    }

    private void ClearChoiceItems(Transform menu, TextMeshProUGUI title, List<Button> buttons, List<Image> backgrounds, List<TextMeshProUGUI> indicators)
    {
        for (int i = menu.childCount - 1; i >= 0; i--)
            if (menu.GetChild(i).gameObject != title.gameObject)
                Destroy(menu.GetChild(i).gameObject);
        buttons.Clear();
        backgrounds.Clear();
        indicators.Clear();
    }

    private static bool IsChoiceInput(Keyboard keyboard)
    {
        return keyboard.upArrowKey.wasPressedThisFrame || keyboard.wKey.wasPressedThisFrame ||
               keyboard.downArrowKey.wasPressedThisFrame || keyboard.sKey.wasPressedThisFrame ||
               keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame ||
               keyboard.spaceKey.wasPressedThisFrame;
    }

    private void HandleChoiceInput(Keyboard keyboard, System.Collections.Generic.IList<Button> buttons, ref int selectedIndex, System.Action refresh)
    {
        int direction = keyboard.upArrowKey.wasPressedThisFrame || keyboard.wKey.wasPressedThisFrame ? -1
            : keyboard.downArrowKey.wasPressedThisFrame || keyboard.sKey.wasPressedThisFrame ? 1 : 0;
        if (direction != 0 && buttons.Count > 0)
        {
            for (int attempt = 0; attempt < buttons.Count; attempt++)
            {
                selectedIndex = (selectedIndex + direction + buttons.Count) % buttons.Count;
                if (buttons[selectedIndex].interactable)
                    break;
            }
            refresh();
        }
        else if (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame)
        {
            if (selectedIndex >= 0 && selectedIndex < buttons.Count && buttons[selectedIndex].interactable)
                buttons[selectedIndex].onClick.Invoke();
        }
    }

    private void SelectEnemyTarget(EnemyState target)
    {
        if (!CanAct() || target == null || target.HP <= 0)
        {
            CloseEnemyTargetMenu(true);
            SetMessage("That enemy is no longer a valid target.");
            RefreshUI();
            return;
        }

        System.Action<EnemyState> targetAction = pendingEnemyTargetAction;
        CloseEnemyTargetMenu(false);
        targetAction?.Invoke(target);
    }

    private void CloseEnemyTargetMenu(bool restoreSpellMenu)
    {
        isTargetMenuOpen = false;
        pendingEnemyTargetAction = null;
        if (targetMenuRoot != null)
            targetMenuRoot.SetActive(false);

        if (restoreSpellMenu && returnToSpellMenuAfterTarget)
        {
            isSpellMenuOpen = true;
            spellMenuRoot.SetActive(true);
        }
        else
        {
            isSpellMenuOpen = false;
            if (spellMenuRoot != null)
                spellMenuRoot.SetActive(false);
        }
        returnToSpellMenuAfterTarget = false;
        controlsText.text = "↑ / ↓ Select     Enter Confirm";
        SetButtons(CanAct());
        if (isSpellMenuOpen)
            RefreshSpellSelection();
    }

    private void RefreshTargetSelection()
    {
        RefreshChoiceSelection(targetButtons, targetBackgrounds, targetIndicators, selectedTargetIndex);
    }

    private void RefreshChoiceSelection(System.Collections.Generic.IList<Button> buttons, System.Collections.Generic.IList<Image> backgrounds, System.Collections.Generic.IList<TextMeshProUGUI> indicators, int selectedIndex)
    {
        for (int i = 0; i < buttons.Count; i++)
        {
            bool selected = i == selectedIndex && buttons[i].interactable;
            backgrounds[i].color = selected
                ? new Color(0.075f, 0.23f, 0.37f, 1f)
                : buttons[i].interactable ? PanelColor : new Color(0.035f, 0.045f, 0.065f, 0.75f);
            indicators[i].text = selected ? "▶" : string.Empty;
            indicators[i].color = selected ? AccentColor : MutedColor;
        }
    }

    private void OpenSpellMenu(PartyMemberState user, List<SkillDefinition> skills)
    {
        isSpellMenuOpen = true;
        selectedSpellIndex = 0;
        spellMenuTitle.text = user.Character.DisplayName + " - Spells (" + user.CurrentMP + " MP)";
        ClearChoiceItems(spellMenuRoot.transform, spellMenuTitle, spellButtons, spellBackgrounds, spellIndicators);

        for (int i = 0; i < skills.Count; i++)
        {
            SkillDefinition skill = skills[i];
            bool canCast = user.CurrentMP >= skill.MPCost;
            UnityEngine.Events.UnityAction selectSkill = skill.Effect == SkillEffect.DamageEnemy
                ? (UnityEngine.Events.UnityAction)(() => OpenEnemyTargetMenu(target => CastSkill(skill, target), true))
                : () => CastSkill(skill);
            CreateSpellButton(skill.DisplayName, skill.MPCost + " MP", i, skills.Count + 1, canCast, selectSkill);
        }
        CreateSpellButton("Back", "Esc", skills.Count, skills.Count + 1, true, CloseSpellMenu);
        selectedSpellIndex = spellButtons.FindIndex(button => button.interactable);
        spellMenuRoot.SetActive(true);
        SetButtons(CanAct());
        RefreshSpellSelection();
    }

    private void CreateSpellButton(string label, string cost, int index, int rowCount, bool interactable, UnityEngine.Events.UnityAction action)
    {
        GameObject buttonObject = CreateChoiceButton(spellMenuRoot.transform, label, index, rowCount, interactable, action, spellButtons, spellBackgrounds, spellIndicators, "Spell Name");
        TextMeshProUGUI spellLabel = buttonObject.transform.Find("Spell Name").GetComponent<TextMeshProUGUI>();
        spellLabel.rectTransform.anchorMax = new Vector2(0.73f, 1f);
        TextMeshProUGUI spellCost = CreateText("Spell Cost", buttonObject.transform, new Vector2(0.72f, 0f), new Vector2(0.98f, 1f), 12, TextAnchor.MiddleRight, MutedColor, FontStyle.Normal, battleOptionsFont);
        spellCost.text = cost;
    }

    private void CloseSpellMenu()
    {
        isSpellMenuOpen = false;
        if (spellMenuRoot != null)
            spellMenuRoot.SetActive(false);
        selectedActionIndex = 1;
        if (battlePanel != null)
            SetButtons(CanAct());
    }

    private void RefreshSpellSelection()
    {
        RefreshChoiceSelection(spellButtons, spellBackgrounds, spellIndicators, selectedSpellIndex);
    }

    private void BuildPartyRows()
    {
        foreach (PartyMemberUI previousRow in partyRows)
        {
            if (previousRow.root != null)
                Destroy(previousRow.root);
            if (previousRow.battleSpriteRoot != null)
                Destroy(previousRow.battleSpriteRoot.gameObject);
        }
        partyRows.Clear();
        if (gameState == null || partyStatusRoot == null) return;
        int count = gameState.Party.Count;
        float gap = 0.012f;
        float width = count == 0 ? 0f : (1f - gap * (count - 1)) / count;
        for (int i = 0; i < count; i++)
        {
            PartyMemberState member = gameState.Party[i];
            float left = i * (width + gap);
            GameObject row = CreateRect("Party Member " + (i + 1), partyStatusRoot, new Vector2(left, 0f), new Vector2(left + width, 1f), FrameColor);
            GameObject panel = CreateRect("Party Member Inner", row.transform, Vector2.zero, Vector2.one, new Color(0.025f, 0.075f, 0.12f, 0.96f));
            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.offsetMin = new Vector2(2f, 2f);
            panelRect.offsetMax = new Vector2(-2f, -2f);
            Outline outline = row.AddComponent<Outline>();
            outline.effectDistance = new Vector2(1f, 1f);
            outline.effectColor = new Color(0.35f, 0.67f, 0.86f, 0.8f);

            Vector2 spriteAnchor = new Vector2(0.55f + (i % 2) * 0.11f, 0.39f + (i / 2) * 0.16f);
            Sprite battler = member != null && member.Character != null
                ? (member.Character.BattleSprite != null ? member.Character.BattleSprite : member.Character.Portrait)
                : null;
            Image stageSprite;
            RectTransform stageRoot = CreateStageSprite("Party Battler " + (i + 1), battlePanel.transform, spriteAnchor, new Vector2(150f, 210f), battler, out stageSprite);

            PartyMemberUI ui = new PartyMemberUI
            {
                root = row,
                background = panel.GetComponent<Image>(),
                outline = outline,
                turnIndicator = CreateText("Turn Indicator", panel.transform, new Vector2(0.02f, 0.78f), new Vector2(0.15f, 0.98f), 13, TextAnchor.MiddleCenter, AccentColor, FontStyle.Bold),
                nameText = CreateText("Name", panel.transform, new Vector2(0.14f, 0.78f), new Vector2(0.98f, 0.98f), 15, TextAnchor.MiddleLeft, Color.white, FontStyle.Bold, statusFont),
                statsText = CreateText("Stats", panel.transform, new Vector2(0.04f, 0.58f), new Vector2(0.98f, 0.77f), 10, TextAnchor.MiddleLeft, MutedColor, FontStyle.Normal),
                hpText = CreateText("HP Value", panel.transform, new Vector2(0.04f, 0.36f), new Vector2(0.98f, 0.57f), 12, TextAnchor.MiddleLeft, Color.white, FontStyle.Normal, statusFont),
                mpText = CreateText("MP Value", panel.transform, new Vector2(0.04f, 0.12f), new Vector2(0.98f, 0.34f), 12, TextAnchor.MiddleLeft, AccentColor, FontStyle.Normal, statusFont),
                hpFill = CreateGauge(panel.transform, "HP", new Vector2(0.04f, 0.3f), new Vector2(0.96f, 0.34f), new Color(0.24f, 0.86f, 0.48f, 1f)),
                mpFill = CreateGauge(panel.transform, "MP", new Vector2(0.04f, 0.06f), new Vector2(0.96f, 0.1f), new Color(0.28f, 0.65f, 1f, 1f)),
                battleSprite = stageSprite,
                battleSpriteRoot = stageRoot,
                battleSpriteTarget = stageRoot.anchoredPosition
            };
            partyRows.Add(ui);
        }
    }

    private Image CreateGauge(Transform parent, string label, Vector2 min, Vector2 max, Color fillColor)
    {
        GameObject background = CreateRect(label + " Gauge", parent, min, max, new Color(0.005f, 0.018f, 0.035f, 1f));
        GameObject fill = CreateRect(label + " Fill", background.transform, Vector2.zero, Vector2.one, fillColor);
        return fill.GetComponent<Image>();
    }

    private void BuildEnemyRows()
    {
        if (enemyListRoot == null) return;
        for (int i = enemyListRoot.childCount - 1; i >= 0; i--)
            Destroy(enemyListRoot.GetChild(i).gameObject);
        enemyRows.Clear();
        int count = enemies.Count;
        if (count == 0) return;

        for (int i = 0; i < count; i++)
        {
            EnemyDefinition definition = enemies[i].Definition;
            int column = i % 2;
            int rowIndex = i / 2;
            Vector2 anchor = new Vector2(0.2f + column * 0.15f, 0.42f + rowIndex * 0.2f);
            Image stageSprite;
            RectTransform root = CreateStageSprite("Enemy Battler " + (i + 1), enemyListRoot, anchor, new Vector2(210f, 260f), definition.Artwork, out stageSprite);
            TextMeshProUGUI nameText = CreateText("Enemy Name", root, new Vector2(0f, 0.12f), new Vector2(1f, 0.23f), 15, TextAnchor.MiddleCenter, Color.white, FontStyle.Bold, statusFont);
            TextMeshProUGUI hpText = CreateText("Enemy HP", root, new Vector2(0f, 0.045f), new Vector2(1f, 0.13f), 12, TextAnchor.MiddleCenter, Color.white, FontStyle.Normal, statusFont);
            Image hpFill = CreateGauge(root, "Enemy HP", new Vector2(0.08f, 0.015f), new Vector2(0.92f, 0.045f), new Color(0.24f, 0.86f, 0.48f, 1f));
            EnemyUI ui = new EnemyUI
            {
                root = root.gameObject,
                battleSprite = stageSprite,
                nameText = nameText,
                hpText = hpText,
                hpFill = hpFill
            };
            enemyRows.Add(ui);
        }
    }

    private void RefreshActionSelection()
    {
        RefreshChoiceSelection(actionButtons, actionBackgrounds, actionIndicators, selectedActionIndex);
    }

    private void RefreshUI()
    {
        if (gameState == null || partyRows.Count == 0) return;
        for (int i = 0; i < partyRows.Count && i < gameState.Party.Count; i++)
        {
            PartyMemberState member = gameState.Party[i];
            PartyMemberUI row = partyRows[i];
            if (member == null || member.Character == null)
            {
                row.root.SetActive(false);
                if (row.battleSpriteRoot != null)
                    row.battleSpriteRoot.gameObject.SetActive(false);
                continue;
            }
            row.root.SetActive(true);
            if (row.battleSpriteRoot != null)
                row.battleSpriteRoot.gameObject.SetActive(true);
            row.nameText.text = member.Character.DisplayName;
            row.statsText.text = "Lv " + member.Level + "  ATK " + member.AttackPower + "  DEF " + member.DefensePower;
            row.hpText.text = "HP  " + member.CurrentHP + " / " + member.MaxHP;
            row.mpText.text = "MP  " + member.CurrentMP + " / " + member.MaxMP;
            SetGauge(row.hpFill, member.CurrentHP, member.MaxHP);
            SetGauge(row.mpFill, member.CurrentMP, member.MaxMP);
            bool isCurrent = isBattleActive && !isResolvingTurn && !isTransitioning && member.IsAlive && i == currentMemberIndex;
            row.turnIndicator.text = isCurrent ? "▶" : string.Empty;
            row.background.color = isCurrent
                ? new Color(0.07f, 0.16f, 0.25f, 1f)
                : member.IsAlive ? new Color(0.025f, 0.075f, 0.12f, 0.96f) : new Color(0.055f, 0.055f, 0.07f, 0.85f);
            row.outline.effectColor = isCurrent ? AccentColor : new Color(0.35f, 0.67f, 0.86f, 0.8f);
            row.nameText.color = member.IsAlive ? Color.white : MutedColor;
            if (row.battleSprite.enabled)
                row.battleSprite.color = member.IsAlive ? Color.white : new Color(0.48f, 0.48f, 0.52f, 0.8f);
        }
        for (int i = 0; i < enemyRows.Count && i < enemies.Count; i++)
        {
            EnemyState enemy = enemies[i];
            EnemyUI row = enemyRows[i];
            row.nameText.text = enemy.Definition.DisplayName;
            row.hpText.text = enemy.HP + " / " + enemy.Definition.MaxHP + " HP";
            SetGauge(row.hpFill, enemy.HP, enemy.Definition.MaxHP);
            row.nameText.color = enemy.HP > 0 ? Color.white : MutedColor;
            row.hpText.color = enemy.HP > 0 ? MutedColor : new Color(0.45f, 0.48f, 0.52f, 1f);
            if (row.battleSprite.enabled)
                row.battleSprite.color = enemy.HP > 0 ? Color.white : new Color(0.45f, 0.45f, 0.48f, 0.7f);
        }
        SetButtons(CanAct());
    }

    private void SetGauge(Image fill, int current, int maximum)
    {
        RectTransform rect = fill.rectTransform;
        float ratio = maximum <= 0 ? 0f : Mathf.Clamp01((float)current / maximum);
        rect.anchorMax = new Vector2(ratio, 1f);
        rect.offsetMax = Vector2.zero;
    }

    private void SetButtons(bool interactable)
    {
        if (actionButtons[0] == null) return;
        for (int i = 0; i < actionButtons.Length; i++)
        {
            actionButtons[i].gameObject.SetActive(!isSpellMenuOpen && !isTargetMenuOpen);
            actionButtons[i].interactable = interactable && !isSpellMenuOpen && !isTargetMenuOpen && (i != 2 || HasUsableItem());
        }
        for (int i = 0; i < targetButtons.Count; i++)
            targetButtons[i].interactable = interactable && isTargetMenuOpen;
        if (isSpellMenuOpen && spellButtons.Count > 0)
            spellButtons[spellButtons.Count - 1].interactable = interactable;
        if (!isSpellMenuOpen && !isTargetMenuOpen && !actionButtons[selectedActionIndex].interactable)
        {
            for (int i = 0; i < actionButtons.Length; i++)
            {
                if (actionButtons[i].interactable)
                {
                    selectedActionIndex = i;
                    break;
                }
            }
        }
        RefreshActionSelection();
        if (isSpellMenuOpen)
            RefreshSpellSelection();
        if (isTargetMenuOpen)
            RefreshTargetSelection();
    }

    private bool HasUsableItem()
    {
        if (gameState == null) return false;
        foreach (InventoryStack stack in gameState.Inventory)
        {
            if (stack != null && stack.Item != null && stack.Quantity > 0 && stack.Item.Category == ItemCategory.Consumable &&
                stack.Item.Effect != ItemEffect.None)
                return true;
        }
        return false;
    }

    private void SetMessage(string message)
    {
        if (messageText != null)
            messageText.text = message;
    }
}
