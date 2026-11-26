using TMPro;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public sealed class MerchantShop : MonoBehaviour
{
    [System.Serializable]
    private sealed class UpgradeRow
    {
        public Button button;
        public Image accent;
        public TMP_Text level;
        public Sprite preview;
    }

    private enum Stat { Health, Energy, EnergyRecovery, Attack, FlyingKick }

    private const int MaxLevel = 5;
    private const float BarGrowth = 36f;
    private const float FeedbackDuration = 0.7f;
    private const float CoinDuration = 0.55f;
    private const float ShakeDuration = 0.32f;
    private static readonly string[] StatNames =
        { "Max HP", "Max Energy", "Energy Recovery", "Attack Damage", "Flying Kick Damage" };
    private static readonly string[] StatUnits = { " HP", " energy", " energy/sec", " damage", " damage" };
    private static readonly int[] StatIncreases = { 20, 20, 3, 5, 5 };
    private static readonly int[] Prices = { 100, 200, 350, 500 };
    private static readonly int Talking = Animator.StringToHash("IsTalking");
    private static readonly Color PurchaseColor = new Color32(249, 198, 111, 255);
    private static readonly Color UnavailableColor = new Color32(182, 200, 212, 255);
    private static readonly Color SuccessColor = new Color32(146, 224, 200, 255);
    private static readonly Color WarningColor = new Color32(255, 151, 137, 255);

    [SerializeField] private PlayerHUDController hud;
    [SerializeField] private CharacterCombat combat;
    [SerializeField] private PlayerMovement movement;
    [SerializeField] private PlayerInteractionManager interaction;
    [SerializeField] private ThirdPersonCameraLook cameraLook;
    [SerializeField] private CinemachineBrain cameraBrain;
    [SerializeField] private CinemachineCamera shopCamera;
    [SerializeField] private Animator merchantAnimator;
    [SerializeField] private RectTransform healthBar;
    [SerializeField] private RectTransform energyBar;
    [SerializeField] private GameObject panel;
    [SerializeField] private RectTransform window;
    [SerializeField] private UpgradeRow[] rows;
    [SerializeField] private TMP_Text balance;
    [SerializeField] private TMP_Text coinChange;
    [SerializeField] private TMP_Text feedback;
    [SerializeField] private TMP_Text statName;
    [SerializeField] private TMP_Text levelChange;
    [SerializeField] private TMP_Text statChange;
    [SerializeField] private TMP_Text purchaseLabel;
    [SerializeField] private Image preview;
    [SerializeField] private GameObject barComparison;
    [SerializeField] private Image[] comparisonFrames;
    [SerializeField] private Image[] comparisonFills;
    [SerializeField] private TMP_Text[] comparisonLabels;
    [SerializeField] private Image previewFlash;
    [SerializeField] private Button purchase;
    [SerializeField] private Button close;

    private static readonly int[] purchases = new int[StatNames.Length];

    internal static void ResetUpgrades() => System.Array.Clear(purchases, 0, purchases.Length);
    private Stat selected;
    private bool open;
    private float previousTimeScale;
    private CursorLockMode previousCursorLock;
    private bool previousCursorVisibility;
    private bool interactionEnabled;
    private bool cameraEnabled;
    private bool previousIgnoreTimeScale;
    private AnimatorUpdateMode originalAnimatorMode;
    private Quaternion originalMerchantRotation;
    private Vector2 purchasePosition;
    private Vector2 coinChangePosition;
    private int balanceFrom;
    private int balanceTo;
    private int displayedBalance;
    private float balanceElapsed;
    private float shakeElapsed;
    private float feedbackElapsed;
    private bool purchaseSucceeded;

    internal bool CanOpen => isActiveAndEnabled && !open && hud != null && hud.IsVisible &&
        combat != null && combat.CanAct && !combat.IsAttacking && !combat.IsBlocking &&
        movement != null && movement.enabled && Time.timeScale > 0f;

    private void Awake()
    {
        panel.SetActive(false);
        for (int i = 0; i < rows.Length; i++)
        {
            Stat stat = (Stat)i;
            rows[i].button.onClick.AddListener(() => Select(stat));
        }
        purchase.onClick.AddListener(Purchase);
        close.onClick.AddListener(Close);
        purchasePosition = ((RectTransform)purchase.transform).anchoredPosition;
        coinChangePosition = coinChange.rectTransform.anchoredPosition;
    }

    internal void Open()
    {
        if (!CanOpen)
            return;

        open = true;
        previousTimeScale = Time.timeScale;
        previousCursorLock = Cursor.lockState;
        previousCursorVisibility = Cursor.visible;
        interactionEnabled = interaction.enabled;
        cameraEnabled = cameraLook.enabled;
        interaction.enabled = false;
        movement.enabled = false;
        cameraLook.enabled = false;
        Time.timeScale = 0f;
        StartMerchantView();
        hud.SetShopActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        panel.SetActive(true);
        FitWindow();
        displayedBalance = balanceFrom = balanceTo = hud.Coins;
        balanceElapsed = CoinDuration;
        coinChange.gameObject.SetActive(false);
        RenderBalance();
        ResetFeedback();
        Select(Stat.Health);
        rows[0].button.Select();
    }

    private void Update()
    {
        if (!open)
            return;

        AnimateFeedback();
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            Close();
            return;
        }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKeyDown(KeyCode.Escape))
            Close();
#endif
    }

    private void OnDisable() => Close();

    private void Close()
    {
        if (!open)
            return;

        open = false;
        panel.SetActive(false);
        EndMerchantView();
        Time.timeScale = previousTimeScale;
        if (movement != null)
            movement.enabled = true;
        if (interaction != null)
            interaction.enabled = interactionEnabled;
        if (cameraLook != null)
            cameraLook.enabled = cameraEnabled;
        if (hud != null)
            hud.SetShopActive(false);
        Cursor.lockState = previousCursorLock;
        Cursor.visible = previousCursorVisibility;
    }

    private void Select(Stat stat)
    {
        ResetFeedback();
        selected = stat;
        Refresh();
    }

    private void Purchase()
    {
        if (!open)
            return;

        int index = (int)selected;
        int tier = purchases[index];
        if (tier >= Prices.Length || !CanUpgrade(selected))
            return;

        int price = Prices[tier];
        if (!hud.TrySpendCoins(price))
        {
            ShowFeedback("Not enough coins", false);
            return;
        }

        int amount = StatIncreases[index];
        switch (selected)
        {
            case Stat.Health:
                combat.IncreaseMaxHealth(amount);
                GrowBar(healthBar);
                break;
            case Stat.Energy:
                movement.IncreaseMaxEnergy(amount / 100f);
                GrowBar(energyBar);
                break;
            case Stat.EnergyRecovery:
                movement.IncreaseEnergyRecovery(amount / 100f);
                break;
            case Stat.Attack:
            case Stat.FlyingKick:
                combat.AttackHitbox.IncreaseDamage(amount, selected == Stat.FlyingKick);
                break;
        }

        purchases[index]++;
        balanceFrom = displayedBalance;
        balanceTo = hud.Coins;
        balanceElapsed = 0;
        coinChange.text = $"−{price:N0}";
        coinChange.gameObject.SetActive(true);
        ShowFeedback("Upgrade purchased", true);
        Refresh();
        if (!purchase.interactable)
            rows[index].button.Select();
    }

    private bool CanUpgrade(Stat stat) => stat switch
    {
        Stat.Energy => movement.MaxEnergy > 0f,
        Stat.Attack or Stat.FlyingKick => combat.AttackHitbox != null,
        _ => true
    };

    private float GetStatValue(Stat stat) => stat switch
    {
        Stat.Health => combat.MaxHealth,
        Stat.Energy => movement.MaxEnergy * 100f,
        Stat.EnergyRecovery => movement.EnergyRecoveryPerSecond * 100f,
        Stat.Attack => combat.AttackHitbox != null ? combat.AttackHitbox.AttackDamage : 0f,
        Stat.FlyingKick => combat.AttackHitbox != null ? combat.AttackHitbox.FlyingKickDamage : 0f,
        _ => 0f
    };

    private void Refresh()
    {
        int index = (int)selected;
        int level = purchases[index] + 1;
        bool atMax = level == MaxLevel;
        bool available = CanUpgrade(selected);

        for (int i = 0; i < rows.Length; i++)
        {
            rows[i].level.text = $"Lv. {purchases[i] + 1}";
            rows[i].accent.enabled = i == index;
        }

        bool showBars = selected is Stat.Health or Stat.Energy;
        preview.enabled = !showBars;
        barComparison.SetActive(showBars);
        if (showBars)
            RefreshBarComparison(selected == Stat.Health ? healthBar : energyBar, atMax);
        else
            preview.sprite = rows[index].preview;
        statName.text = StatNames[index];
        levelChange.text = atMax ? $"Lv. {level} · MAX" : $"Lv. {level}  →  {level + 1}";
        float current = GetStatValue(selected);
        string units = StatUnits[index];
        statChange.text = atMax ? $"{current:0.##}{units}" :
            $"{current:0.##}  →  {current + StatIncreases[index]:0.##}{units}";
        purchase.interactable = available && !atMax;
        purchaseLabel.color = purchase.interactable ? PurchaseColor : UnavailableColor;
        purchaseLabel.text = !available ? "Unavailable" : atMax ? "Fully upgraded" :
            $"Upgrade  ·  {Prices[level - 1]:N0} coins";
    }

    private void RefreshBarComparison(RectTransform source, bool atMax)
    {
        Sprite frame = source.Find("Background").GetComponent<Image>().sprite;
        Sprite fill = source.GetComponent<Slider>().fillRect.GetComponent<Image>().sprite;
        comparisonLabels[0].text = atMax ? "MAX CAPACITY" : "CURRENT";
        comparisonLabels[1].gameObject.SetActive(!atMax);
        comparisonFrames[1].gameObject.SetActive(!atMax);

        const float nextWidth = 490f;
        float currentWidth = source.rect.width;
        for (int i = 0; i < comparisonFrames.Length; i++)
        {
            comparisonFrames[i].sprite = frame;
            comparisonFills[i].sprite = fill;
            float width = i == 0 && !atMax ? nextWidth * currentWidth / (currentWidth + BarGrowth) : nextWidth;
            comparisonFrames[i].rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
        }
    }

    private void StartMerchantView()
    {
        previousIgnoreTimeScale = cameraBrain.IgnoreTimeScale;
        cameraBrain.IgnoreTimeScale = true;
        shopCamera.gameObject.SetActive(true);

        originalAnimatorMode = merchantAnimator.updateMode;
        originalMerchantRotation = merchantAnimator.transform.rotation;
        Vector3 towardCamera = Vector3.ProjectOnPlane(
            shopCamera.transform.position - merchantAnimator.transform.position, Vector3.up);
        if (towardCamera.sqrMagnitude > 0.001f)
            merchantAnimator.transform.rotation = Quaternion.LookRotation(towardCamera);

        merchantAnimator.updateMode = AnimatorUpdateMode.UnscaledTime;
        merchantAnimator.SetBool(Talking, true);
    }

    private void EndMerchantView()
    {
        if (shopCamera != null)
            shopCamera.gameObject.SetActive(false);
        if (cameraBrain != null)
            cameraBrain.IgnoreTimeScale = previousIgnoreTimeScale;
        if (merchantAnimator != null)
        {
            merchantAnimator.SetBool(Talking, false);
            merchantAnimator.updateMode = originalAnimatorMode;
            merchantAnimator.transform.rotation = originalMerchantRotation;
        }
    }

    private void OnRectTransformDimensionsChange()
    {
        if (open)
            FitWindow();
    }

    private void FitWindow()
    {
        Rect bounds = ((RectTransform)window.parent).rect;
        float scale = Mathf.Min(1f, (bounds.width * .65f - 32f) / window.rect.width,
            (bounds.height - 64f) / window.rect.height);
        window.localScale = Vector3.one * Mathf.Max(0.1f, scale);
    }

    private void ShowFeedback(string message, bool succeeded)
    {
        ResetFeedback();
        purchaseSucceeded = succeeded;
        feedbackElapsed = 0f;
        shakeElapsed = succeeded ? ShakeDuration : 0f;
        feedback.text = message;
        feedback.color = succeeded ? SuccessColor : WarningColor;
    }

    private void ResetFeedback()
    {
        feedbackElapsed = FeedbackDuration;
        shakeElapsed = ShakeDuration;
        feedback.text = string.Empty;
        previewFlash.color = Color.clear;
        ((RectTransform)purchase.transform).anchoredPosition = purchasePosition;
    }

    private void AnimateFeedback()
    {
        float dt = Time.unscaledDeltaTime;
        if (balanceElapsed < CoinDuration)
        {
            balanceElapsed = Mathf.Min(balanceElapsed + dt, CoinDuration);
            float t = balanceElapsed / CoinDuration;
            displayedBalance = (int)System.Math.Round(balanceFrom + ((double)balanceTo - balanceFrom) *
                Mathf.SmoothStep(0f, 1f, t));
            RenderBalance();
            coinChange.rectTransform.anchoredPosition = coinChangePosition + Vector2.down * (t * 20f);
            Color tint = WarningColor;
            tint.a = 1f - Mathf.SmoothStep(0f, 1f, (t - .3f) / .7f);
            coinChange.color = tint;
            if (balanceElapsed == CoinDuration)
                coinChange.gameObject.SetActive(false);
        }

        if (shakeElapsed < ShakeDuration)
        {
            shakeElapsed = Mathf.Min(shakeElapsed + dt, ShakeDuration);
            float t = shakeElapsed / ShakeDuration;
            ((RectTransform)purchase.transform).anchoredPosition = purchasePosition +
                Vector2.right * (Mathf.Sin(t * Mathf.PI * 6f) * (1f - t) * 12f);
        }

        if (feedbackElapsed < FeedbackDuration)
        {
            feedbackElapsed = Mathf.Min(feedbackElapsed + dt, FeedbackDuration);
            if (purchaseSucceeded)
            {
                float pulse = 1f - feedbackElapsed / FeedbackDuration;
                previewFlash.color = new Color(SuccessColor.r, SuccessColor.g, SuccessColor.b, pulse * pulse * .26f);
            }
        }
    }

    private void RenderBalance() => balance.text = $"{displayedBalance:N0} coins";

    private static void GrowBar(RectTransform bar)
    {
        if (bar != null)
            bar.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, bar.rect.width + BarGrowth);
    }
}
