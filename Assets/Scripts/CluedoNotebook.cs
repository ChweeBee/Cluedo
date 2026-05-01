using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CluedoNotebook : MonoBehaviour
{
    [Header("Refs")]
    public CardManager cardManager;

    [Header("UI (auto-built if blank)")]
    public GameObject panel;
    public Transform contentArea;
    public GameObject rowPrefab;

    CluedoPlayer activePlayer;
    bool built;

    void Awake()
    {
        if (cardManager == null) cardManager = FindFirstObjectByType<CardManager>();
        EnsureBuilt();
        Hide();
    }

    public void ShowForPlayer(CluedoPlayer player)
    {
        if (player == null) return;
        activePlayer = player;
        EnsureBuilt();
        BuildRowsFor(player);
        if (panel != null) panel.SetActive(true);
    }

    public void Hide()
    {
        if (panel != null) panel.SetActive(false);
    }

    public bool IsVisible => panel != null && panel.activeSelf;

    public void EnsureBuilt()
    {
        if (built && panel != null && contentArea != null) return;

        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            var canvasGO = new GameObject("NotebookCanvas",
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;
        }

        if (panel == null)
        {
            panel = new GameObject("NotebookPanel",
                typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(canvas.transform, false);

            var rt = (RectTransform)panel.transform;
            rt.anchorMin = new Vector2(1f, 0f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 0.5f);
            rt.offsetMin = new Vector2(-300f, 20f);
            rt.offsetMax = new Vector2(-20f, -20f);

            panel.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.78f);
        }

        if (contentArea == null)
        {
            var scrollGO = new GameObject("Scroll",
                typeof(RectTransform), typeof(ScrollRect), typeof(Image), typeof(Mask));
            scrollGO.transform.SetParent(panel.transform, false);

            var srt = (RectTransform)scrollGO.transform;
            srt.anchorMin = Vector2.zero;
            srt.anchorMax = Vector2.one;
            srt.offsetMin = new Vector2(8f, 8f);
            srt.offsetMax = new Vector2(-8f, -8f);

            scrollGO.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.02f);
            scrollGO.GetComponent<Mask>().showMaskGraphic = false;

            var contentGO = new GameObject("Content",
                typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            contentGO.transform.SetParent(scrollGO.transform, false);
            contentArea = contentGO.transform;

            var crt = (RectTransform)contentArea;
            crt.anchorMin = new Vector2(0f, 1f);
            crt.anchorMax = new Vector2(1f, 1f);
            crt.pivot = new Vector2(0.5f, 1f);
            crt.anchoredPosition = Vector2.zero;
            crt.sizeDelta = new Vector2(0f, 0f);

            var vlg = contentGO.GetComponent<VerticalLayoutGroup>();
            vlg.spacing = 4f;
            vlg.padding = new RectOffset(8, 8, 8, 8);
            vlg.childAlignment = TextAnchor.UpperLeft;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            var csf = contentGO.GetComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var sr = scrollGO.GetComponent<ScrollRect>();
            sr.content = (RectTransform)contentArea;
            sr.horizontal = false;
            sr.vertical = true;
            sr.movementType = ScrollRect.MovementType.Clamped;
        }

        built = true;
    }

    void BuildRowsFor(CluedoPlayer player)
    {
        if (contentArea == null || cardManager == null) return;

        foreach (Transform child in contentArea) Destroy(child.gameObject);

        BuildHeader(player.character + "'s Notebook");

        List<Card> source = cardManager.fullDeck != null && cardManager.fullDeck.Count > 0
            ? cardManager.fullDeck
            : cardManager.allCards;

        List<Card> sorted = source
            .Where(c => c != null)
            .OrderBy(c => (int)c.cardType)
            .ThenBy(c => c.cardName)
            .ToList();

        Card.CardType? lastType = null;
        foreach (Card card in sorted)
        {
            if (lastType != card.cardType)
            {
                BuildHeader(SectionTitle(card.cardType));
                lastType = card.cardType;
            }

            GameObject row = rowPrefab != null
                ? Instantiate(rowPrefab, contentArea)
                : BuildProceduralRow(contentArea);
            row.transform.localScale = Vector3.one;

            string cardName = card.cardName;
            bool initial = player.IsCardChecked(cardName);

            NotebookRow rowScript = row.GetComponent<NotebookRow>();
            if (rowScript != null)
                rowScript.Setup(cardName, initial, isChecked => OnRowToggled(cardName, isChecked));
        }
    }

    void OnRowToggled(string cardName, bool isChecked)
    {
        if (activePlayer == null) return;
        activePlayer.MarkCardChecked(cardName, isChecked);
    }

    static string SectionTitle(Card.CardType type)
    {
        switch (type)
        {
            case Card.CardType.Suspect: return "Suspects";
            case Card.CardType.Weapon: return "Weapons";
            case Card.CardType.Room: return "Rooms";
            default: return type.ToString();
        }
    }

    void BuildHeader(string title)
    {
        var headerGO = new GameObject("Header_" + title,
            typeof(RectTransform), typeof(LayoutElement), typeof(TextMeshProUGUI));
        headerGO.transform.SetParent(contentArea, false);

        var le = headerGO.GetComponent<LayoutElement>();
        le.minHeight = 26f;

        var label = headerGO.GetComponent<TextMeshProUGUI>();
        label.text = title;
        label.fontSize = 18f;
        label.fontStyle = FontStyles.Bold;
        label.color = new Color(1f, 0.85f, 0.4f);
        label.alignment = TextAlignmentOptions.Left;
    }

    GameObject BuildProceduralRow(Transform parent)
    {
        var rowGO = new GameObject("Row",
            typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        rowGO.transform.SetParent(parent, false);

        var hlg = rowGO.GetComponent<HorizontalLayoutGroup>();
        hlg.spacing = 8f;
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;

        var rowLE = rowGO.GetComponent<LayoutElement>();
        rowLE.minHeight = 26f;

        var toggleGO = new GameObject("Check",
            typeof(RectTransform), typeof(Toggle), typeof(LayoutElement));
        toggleGO.transform.SetParent(rowGO.transform, false);
        var tle = toggleGO.GetComponent<LayoutElement>();
        tle.minWidth = 22f; tle.minHeight = 22f;
        tle.preferredWidth = 22f; tle.preferredHeight = 22f;

        var bgGO = new GameObject("Background", typeof(RectTransform), typeof(Image));
        bgGO.transform.SetParent(toggleGO.transform, false);
        var bgRT = (RectTransform)bgGO.transform;
        bgRT.anchorMin = Vector2.zero; bgRT.anchorMax = Vector2.one;
        bgRT.offsetMin = Vector2.zero; bgRT.offsetMax = Vector2.zero;
        var bgImg = bgGO.GetComponent<Image>();
        bgImg.color = Color.white;

        var checkGO = new GameObject("Checkmark", typeof(RectTransform), typeof(Image));
        checkGO.transform.SetParent(bgGO.transform, false);
        var ckRT = (RectTransform)checkGO.transform;
        ckRT.anchorMin = new Vector2(0.18f, 0.18f);
        ckRT.anchorMax = new Vector2(0.82f, 0.82f);
        ckRT.offsetMin = Vector2.zero; ckRT.offsetMax = Vector2.zero;
        var ckImg = checkGO.GetComponent<Image>();
        ckImg.color = Color.black;

        var toggle = toggleGO.GetComponent<Toggle>();
        toggle.targetGraphic = bgImg;
        toggle.graphic = ckImg;
        toggle.isOn = false;

        var labelGO = new GameObject("Label",
            typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
        labelGO.transform.SetParent(rowGO.transform, false);
        var label = labelGO.GetComponent<TextMeshProUGUI>();
        label.fontSize = 16f;
        label.alignment = TextAlignmentOptions.Left;
        label.color = Color.white;
        label.enableWordWrapping = false;

        var lle = labelGO.GetComponent<LayoutElement>();
        lle.flexibleWidth = 1f;

        var nbr = rowGO.AddComponent<NotebookRow>();
        nbr.label = label;
        nbr.checkbox = toggle;

        return rowGO;
    }
}
