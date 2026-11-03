using System.Globalization;
using DrawnUi.Draw;
using DrawnUi.Models;
using DrawnUi.Views;
using SlintStruct = global::Slint.Struct;

namespace DrawnUi.Slint.Net;

/// <summary>
/// Screen readers for a Slint view: the canvas' accessibility snapshot as <c>DrawnUiNode</c> rows of
/// <c>DrawnUiHost.nodes</c>, which <c>drawnui.slint</c> shows as accessible elements over the drawn controls, and a
/// reader's press and step up / down back to the controls.
/// </summary>
internal static class SlintAccessibility
{
    /// <summary>The rows for the view <paramref name="viewId"/> of <paramref name="snapshot"/>.</summary>
    public static IEnumerable<SlintStruct> Rows(int viewId, AccessibilityNode[] snapshot)
    {
        foreach (var node in snapshot)
        {
            var role = RoleOf(node);
            if (role == 0)
                continue;

            var value = node.Value;
            var checkable = node.IsPressed.HasValue && role is 2 or 3;
            yield return new SlintStruct
            {
                ["view"] = viewId,
                ["id"] = node.Id,
                ["role"] = role,
                // a node whose title text says its name is not named again, the text is read instead
                ["label"] = node.NamedByChild ? string.Empty : node.Label ?? string.Empty,
                ["hint"] = node.Hint ?? string.Empty,
                // a text is read from its value, as Slint's own Text elements are
                ["value"] = value is { } v ? v.Text ?? v.Now.ToString("0.##", CultureInfo.InvariantCulture)
                    : role == 14 && !node.NamedByChild ? node.Label ?? string.Empty : string.Empty,
                ["minimum"] = (float)(value?.Min ?? 0),
                ["maximum"] = (float)(value?.Max ?? 0),
                ["step"] = (float)(value?.Step ?? 0),
                // a control role that takes no input reads as unavailable, as on the other desktop heads
                ["enabled"] = node.CanInteract || !Aria.IsInteractiveRole(node.Role),
                ["checkable"] = checkable,
                ["checked"] = checkable && node.IsPressed == true,
                ["x"] = node.Rect.Left,
                ["y"] = node.Rect.Top,
                ["w"] = Math.Max(0, node.Rect.Width),
                ["h"] = Math.Max(0, node.Rect.Height),
            };
        }
    }

    /// <summary>The role code of <c>DrawnUiNode.role</c> (see <c>drawnui.slint</c>); 0 leaves the node out.</summary>
    private static int RoleOf(AccessibilityNode node) => node.Role switch
    {
        "button" or "link" or "menuitem" or "menuitemcheckbox" or "menuitemradio" => 1,
        "checkbox" or "radio" => 2,
        "switch" => 3,
        "slider" or "scrollbar" or "spinbutton" => 4,
        "progressbar" => 5,
        "textbox" or "searchbox" => 6,
        "combobox" => 7,
        "tab" => 8,
        "tablist" => 9,
        "tabpanel" => 10,
        "list" or "listbox" or "grid" or "menu" or "radiogroup" => 11,
        "listitem" or "option" => 12,
        "img" => 13,
        "text" or "heading" or "status" or "alert" or "tooltip" => 14,
        "group" or "region" or "dialog" or "alertdialog" or "toolbar" or "navigation" or "main" => 15,
        "presentation" or "none" or "separator" => 0,
        _ => string.IsNullOrEmpty(node.Label) ? 0 : 14,
    };

    /// <summary>A reader pressed the node <paramref name="id"/>: the control's activation, as Enter does.</summary>
    public static void Activate(Canvas canvas, int id) => SkiaAccessibilityManager.Activate(Find(canvas, id));

    /// <summary>A reader stepped the range control <paramref name="id"/> up or down.</summary>
    public static void Adjust(Canvas canvas, int id, bool increment) => SkiaAccessibilityManager.Adjust(Find(canvas, id), increment);

    // only when a reader acts: the snapshot's controls are found by their stable id
    private static ISkiaAccessibilityNode? Find(Canvas canvas, int id)
    {
        foreach (var view in canvas.Views)
        {
            if (Find(view, id) is { } found)
                return found;
        }
        return null;
    }

    private static ISkiaAccessibilityNode? Find(SkiaControl control, int id)
    {
        if (control.AccessibilityId == id)
            return control;
        foreach (var child in control.Views)
        {
            if (Find(child, id) is { } found)
                return found;
        }
        return null;
    }
}
