using System.Diagnostics.CodeAnalysis;
using Intersect.Client.Framework.File_Management;
using Intersect.Client.Framework.GenericClasses;
using Intersect.Client.Framework.Graphics;
using Intersect.Client.Framework.Gwen.Control.EventArguments;
using Intersect.Client.Framework.Gwen.ControlInternal;
using Newtonsoft.Json.Linq;

namespace Intersect.Client.Framework.Gwen.Control;


/// <summary>
///     Movable window with title bar.
/// </summary>
public partial class WindowControl : ResizableControl
{

    public enum ControlState
    {

        Active = 0,

        Inactive,

    }

    private readonly CloseButton mCloseButton;

    private readonly Label mTitle;

    private readonly Dragger mTitleBar;

    private Color? mActiveColor;

    private Color? mInactiveColor;

    private GameTexture? mActiveImage;

    private GameTexture? mInactiveImage;

    private string? mActiveImageFilename;

    private string? mInactiveImageFilename;

    private bool mDeleteOnClose;

    private Modal mModal;

    private Base mOldParent;

    public Dragger TitleBar => mTitleBar;

    public Label TitleLabel => mTitle;

    public Padding InnerPanelPadding
    {
        get => _innerPanel?.Padding ?? default;
        set
        {
            if (_innerPanel != default)
            {
                _innerPanel.Padding = value;
            }
        }
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="WindowControl" /> class.
    /// </summary>
    /// <param name="parent">Parent control.</param>
    /// <param name="title">Window title.</param>
    /// <param name="modal">Determines whether the window should be modal.</param>
    /// <param name="name">name of this control</param>
    public WindowControl(Base? parent, string? title = default, bool modal = false, string? name = default) : base(parent, name)
    {
        mTitleBar = new Dragger(this);
        mTitleBar.Height = 24;
        mTitleBar.Padding = Gwen.Padding.Zero;
        mTitleBar.Margin = new Margin(0, 0, 0, 0);
        mTitleBar.Target = this;
        mTitleBar.Dock = Pos.Top;

        mTitle = new Label(mTitleBar);
        mTitle.Alignment = Pos.Left | Pos.CenterV;
        mTitle.Text = title ?? string.Empty;
        mTitle.Dock = Pos.Fill;
        mTitle.Padding = new Padding(8, 4, 0, 0);
        mTitle.TextColor = Skin.Colors.Window.TitleInactive;

        mCloseButton = new CloseButton(mTitleBar, this);
        mCloseButton.SetSize(24, 24);
        mCloseButton.Dock = Pos.Top | Pos.Right;
        mCloseButton.Clicked += CloseButtonPressed;
        mCloseButton.IsTabable = false;

        //Create a blank content control, dock it to the top - Should this be a ScrollControl?
        _innerPanel = new Base(this);
        _innerPanel.Dock = Pos.Fill;
        GetResizer(8).Hide();
        BringToFront();
        IsTabable = false;
        Focus();
        MinimumSize = new Point(100, 40);
        ClampMovement = true;
        KeyboardInputEnabled = false;

        if (modal)
        {
            MakeModal();
        }
    }

    /// <summary>
    ///     Window caption.
    /// </summary>
    public string Title
    {
        get => mTitle.Text;
        set => mTitle.Text = value;
    }

    /// <summary>
    ///     Determines whether the window has close button.
    /// </summary>
    public bool IsClosable
    {
        get => !mCloseButton.IsHidden;
        set
        {
            if (value == mCloseButton.IsVisible)
            {
                return;
            }

            mCloseButton.IsVisible = value;
        }
    }

    /// <summary>
    ///     Determines whether the control should be disposed on close.
    /// </summary>
    public bool DeleteOnClose
    {
        get => mDeleteOnClose;
        set => mDeleteOnClose = value;
    }

    protected override void OnVisibilityChanged(object? sender, VisibilityChangedEventArgs eventArgs)
    {
        base.OnVisibilityChanged(sender, eventArgs);

        if (eventArgs.IsVisible)
        {
            BringToFront();
        }
    }

    /// <summary>
    ///     Indicates whether the control is on top of its parent's children.
    /// </summary>
    public override bool IsOnTop
    {
        get { return Parent.Children.Where(x => x is WindowControl).Last() == this; }
    }

    /// <summary>
    /// If the shadow under the window should be drawn.
    /// </summary>
    public bool DrawShadow { get; set; } = true;

    public override JObject GetJson(bool isRoot = default)
    {
        var obj = base.GetJson(isRoot);
        obj.Add(nameof(DrawShadow), DrawShadow);
        obj.Add("ActiveImage", GetImageFilename(ControlState.Active));
        obj.Add("InactiveImage", GetImageFilename(ControlState.Inactive));
        obj.Add("ActiveColor", Color.ToString(mActiveColor));
        obj.Add("InactiveColor", Color.ToString(mInactiveColor));
        obj.Add("Closable", IsClosable);
        obj.Add("Titlebar", mTitleBar.GetJson());
        obj.Add("Title", mTitle.GetJson());
        obj.Add("CloseButton", mCloseButton.GetJson());
        obj.Add("InnerPanel", _innerPanel.GetJson());

        return base.FixJson(obj);
    }

    public override void LoadJson(JToken obj, bool isRoot = default)
    {
        base.LoadJson(obj);

        var tokenDrawShadow = obj[nameof(DrawShadow)];
        if (tokenDrawShadow != null)
        {
            DrawShadow = (bool)tokenDrawShadow;
        }

        if (obj["ActiveImage"] != null)
        {
            SetImage(
                GameContentManager.Current.GetTexture(
                    Framework.Content.TextureType.Gui, (string)obj["ActiveImage"]
                ), (string)obj["ActiveImage"], ControlState.Active
            );
        }

        if (obj["InactiveImage"] != null)
        {
            SetImage(
                GameContentManager.Current.GetTexture(
                    Framework.Content.TextureType.Gui, (string)obj["InactiveImage"]
                ), (string)obj["InactiveImage"], ControlState.Inactive
            );
        }

        if (!string.IsNullOrWhiteSpace((string)obj["ActiveColor"]))
        {
            mActiveColor = Color.FromString((string)obj["ActiveColor"]);
        }

        if (!string.IsNullOrWhiteSpace((string)obj["InactiveColor"]))
        {
            mInactiveColor = Color.FromString((string)obj["InactiveColor"]);
        }

        if (obj["Closable"] != null)
        {
            IsClosable = (bool)obj["Closable"];
        }

        if (obj["Titlebar"] != null)
        {
            mTitleBar.LoadJson(obj["Titlebar"]);
        }

        if (obj["Title"] != null)
        {
            mTitle.LoadJson(obj["Title"]);
        }

        if (obj["CloseButton"] != null)
        {
            mCloseButton.Alignment = Pos.None;
            mCloseButton.Dock = Pos.None;
            mCloseButton.LoadJson(obj["CloseButton"]);
        }

        if (obj["InnerPanel"] != null)
        {
            _innerPanel.LoadJson(obj["InnerPanel"]);
        }
    }

    public override void ProcessAlignments()
    {
        base.ProcessAlignments();
        mTitleBar.ProcessAlignments();
    }

    public override void DisableResizing()
    {
        base.DisableResizing();
        Padding = new Padding(6, 0, 6, 0);
    }

    public void Close()
    {
        CloseButtonPressed(this, EventArgs.Empty);
    }

    protected virtual void CloseButtonPressed(Base control, EventArgs args)
    {
        IsHidden = true;

        if (mModal != null)
        {
            mModal.DelayedDelete();
            mModal = null;
        }

        if (mDeleteOnClose)
        {
            Parent.RemoveChild(this, true);
        }
    }

    /// <summary>
    ///     Makes the window modal: covers the whole canvas and gets all input.
    /// </summary>
    /// <param name="dim">Determines whether all the background should be dimmed.</param>
    public void MakeModal(bool dim = false)
    {
        if (mModal != null)
        {
            return;
        }

        mModal = new Modal(GetCanvas());
        mOldParent = Parent;
        Parent = mModal;

        if (dim)
        {
            mModal.ShouldDrawBackground = true;
        }
        else
        {
            mModal.ShouldDrawBackground = false;
        }
    }

    public void RemoveModal()
    {
        if (mModal != null)
        {
            Parent = mOldParent;
            GetCanvas().RemoveChild(mModal, false);
            mModal = null;
        }
    }

    /// <summary>
    ///     Renders the control using specified skin.
    /// </summary>
    /// <param name="skin">Skin to use.</param>
    protected override void Render(Skin.Base skin)
    {
        var hasFocus = IsOnTop;
        var textColor = GetTextColor(ControlState.Active);
        if (textColor == null && !hasFocus)
        {
            textColor = GetTextColor(ControlState.Inactive);
        }

        textColor ??= Skin.Colors.Window.TitleInactive;

        mTitle.TextColor = textColor;

        skin.DrawWindow(this, mTitleBar.Bottom, hasFocus);
    }

    /// <summary>
    ///     Renders under the actual control (shadows etc).
    /// </summary>
    /// <param name="skin">Skin to use.</param>
    protected override void RenderUnder(Skin.Base skin)
    {
        base.RenderUnder(skin);

        if (DrawShadow)
        {
            skin.DrawShadow(this);
        }
    }

    public override void Touch()
    {
        base.Touch();
        BringToFront();
    }

    /// <summary>
    ///     Renders the focus overlay.
    /// </summary>
    /// <param name="skin">Skin to use.</param>
    protected override void RenderFocus(Skin.Base skin)
    {
    }

    public Rectangle TitleBarBounds => mTitleBar?.Bounds ?? default;

    public void SetTitleBarHeight(int h)
    {
        mTitleBar.SetSize(mTitleBar.Width, h);
        mTitle.Padding = new Padding(8, (h - Skin.Renderer.MeasureText(mTitle.Font, "L", 1).Y) / 2, 0, 0);
    }

    public void SetCloseButtonSize(int w, int h)
    {
        mCloseButton.SetSize(w, h);
        mCloseButton.MaximumSize = new Point(w, h);
    }

    public void SetCloseButtonImage(GameTexture texture, string fileName, Button.ControlState state)
    {
        mCloseButton.SetImage(texture, fileName, state);
    }

    public void SetFont(GameFont font)
    {
        mTitle.Font = font;
        mTitle.Padding = new Padding(
            8, (mTitleBar.Height - Skin.Renderer.MeasureText(mTitle.Font, "L", 1).Y) / 2, 0, 0
        );
    }

    public void SetTextColor(Color clr, ControlState state)
    {
        switch (state)
        {
            case ControlState.Active:
                mActiveColor = clr;

                break;
            case ControlState.Inactive:
                mInactiveColor = clr;

                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(state), state, null);
        }
    }

    public Color? GetTextColor(ControlState state)
    {
        return state switch
        {
            ControlState.Active => mActiveColor,
            ControlState.Inactive => mInactiveColor,
            _ => throw new ArgumentOutOfRangeException(nameof(state), state, $"Invalid {nameof(ControlState)}"),
        };
    }

    /// <summary>
    ///     Sets the button's image.
    /// </summary>
    /// <param name="textureName">Texture name. Null to remove.</param>
    public void SetImage(GameTexture texture, string fileName, ControlState state)
    {
        switch (state)
        {
            case ControlState.Active:
                mActiveImageFilename = fileName;
                mActiveImage = texture;

                break;
            case ControlState.Inactive:
                mInactiveImageFilename = fileName;
                mInactiveImage = texture;

                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(state), state, null);
        }
    }

    public GameTexture? GetImage(ControlState state)
    {
        switch (state)
        {
            case ControlState.Active:
                return mActiveImage;
            case ControlState.Inactive:
                return mInactiveImage;
            default:
                return null;
        }
    }

    public bool TryGetTexture(ControlState controlState, [NotNullWhen(true)] out GameTexture? texture)
    {
        texture = GetImage(controlState);
        return texture != default;
    }

    public string GetImageFilename(ControlState state)
    {
        switch (state)
        {
            case ControlState.Active:
                return mActiveImageFilename;
            case ControlState.Inactive:
                return mInactiveImageFilename;
            default:
                return null;
        }
    }

}
