﻿using System.Numerics;
using Content.Client.UserInterface.Controls;
using Content.Client.VendingMachines.UI;
using Content.Shared._Funkystation.Medical.SmartFridge;
using Content.Shared.IdentityManagement;
using Content.Shared.Labels.Components;
using Robust.Client.AutoGenerated;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.XAML;
using Robust.Shared.Prototypes;
using FancyWindow = Content.Client.UserInterface.Controls.FancyWindow;

namespace Content.Client._Funkystation.Medical.SmartFridge.UI;

[GenerateTypedNameReferences]
public sealed partial class SmartFridgeMenu : FancyWindow
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;

    private readonly Dictionary<EntProtoId, EntityUid> _dummies = [];

    public event Action<GUIBoundKeyEventArgs, ListData>? OnItemSelected;

    private readonly StyleBoxFlat _styleBox = new() { BackgroundColor = new Color(70, 73, 102) };

    public SmartFridgeMenu()
    {
        MinSize = SetSize = new Vector2(250, 150);
        RobustXamlLoader.Load(this);
        IoCManager.InjectDependencies(this);

        VendingContents.SearchBar = SearchBar;
        VendingContents.DataFilterCondition += DataFilterCondition;
        VendingContents.GenerateItem += GenerateButton;
        VendingContents.ItemKeyBindDown += (args, data) => OnItemSelected?.Invoke(args, data);
    }

    private static bool DataFilterCondition(string filter, ListData data)
    {
        if (data is not VendorItemsListData { ItemText: var text })
            return false;

        return string.IsNullOrEmpty(filter) || text.Contains(filter, StringComparison.CurrentCultureIgnoreCase);
    }

    private void GenerateButton(ListData data, ListContainerButton button)
    {
        if (data is not VendorItemsListData { ItemProtoID: var protoId, ItemText: var text })
            return;

        button.AddChild(new VendingMachineItem(protoId, text));

        button.ToolTip = text;
        button.StyleBoxOverride = _styleBox;
    }

    /// <summary>
    /// Populates the list of available items on the vending machine interface
    /// and sets icons based on their prototypes
    /// </summary>
    public void Populate(List<SmartFridgeInventoryItem> inventory)
    {
        if (inventory.Count == 0 && VendingContents.Visible)
        {
            SearchBar.Visible = false;
            VendingContents.Visible = false;

            var outOfStockLabel = new Label
            {
                Text = Loc.GetString("vending-machine-component-try-eject-out-of-stock"),
                Margin = new Thickness(4, 4),
                HorizontalExpand = true,
                VerticalAlignment = VAlignment.Stretch,
                HorizontalAlignment = HAlignment.Center,
            };

            MainContainer.AddChild(outOfStockLabel);

            SetSizeAfterUpdate(outOfStockLabel.Text.Length, 0);

            return;
        }

        var longestEntry = string.Empty;
        var listData = new List<VendorItemsListData>();

        for (var i = 0; i < inventory.Count; i++)
        {
            var entry = inventory[i];

            if (!_prototypeManager.TryIndex(entry.Id, out var prototype))
                continue;

            if (!_dummies.TryGetValue(entry.Id, out var dummy))
            {
                dummy = _entityManager.Spawn(entry.Id);
                _dummies.Add(entry.Id, dummy);
            }

            var itemText = $"{entry.ItemName} [{entry.Quantity}]";

            if (itemText.Length > longestEntry.Length)
                longestEntry = itemText;

            listData.Add(new VendorItemsListData(prototype.ID, itemText, i));
        }

        VendingContents.PopulateList(listData);

        SetSizeAfterUpdate(longestEntry.Length, inventory.Count);
    }

    private void SetSizeAfterUpdate(int longestEntryLength, int contentCount)
    {
        SetSize = new Vector2(Math.Clamp((longestEntryLength + 2) * 12, 250, 400),
            Math.Clamp(contentCount * 50, 150, 350));
    }
}
