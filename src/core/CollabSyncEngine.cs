using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ZenQOffice.UI;

namespace ZenQOffice.Core
{
    #region Serialization DTOs (Slides / Calc / Writer)

    public class CollabShapeDto
    {
        public string Id { get; set; } = "";
        public string PresetId { get; set; }
        public string Name { get; set; }
        public int Type { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public float Width { get; set; }
        public float Height { get; set; }
        public float Rotation { get; set; }
        public int ZIndex { get; set; }
        public bool FlipHorizontal { get; set; }
        public bool FlipVertical { get; set; }
        public bool IsVisible { get; set; } = true;

        public string Text { get; set; }
        public string FontName { get; set; }
        public float FontSize { get; set; }
        public bool Bold { get; set; }
        public bool Italic { get; set; }
        public bool Underline { get; set; }
        public bool Strikethrough { get; set; }
        public int Alignment { get; set; }
        public int VerticalAlignment { get; set; }
        public int TextColor { get; set; }
        public float LineSpacing { get; set; }

        public bool HasFill { get; set; } = true;
        public int FillColor { get; set; }
        public bool HasGradientFill { get; set; }
        public int GradientColor2 { get; set; }
        public float GradientAngle { get; set; }
        public float FillOpacity { get; set; } = 1f;
        public int StrokeColor { get; set; }
        public float StrokeWidth { get; set; }
        public float StrokeOpacity { get; set; } = 1f;
        public float CornerRadius { get; set; } = -1f;

        public string ImageAssetKey { get; set; } = "";
        public string ImagePath { get; set; } = "";
        public float CropLeft { get; set; }
        public float CropTop { get; set; }
        public float CropRight { get; set; }
        public float CropBottom { get; set; }

        public int TableRows { get; set; }
        public int TableColumns { get; set; }
        public List<List<string>> TableData { get; set; }

        public List<CollabSlideRunDto> TextRuns { get; set; }
    }

    public class CollabSlideRunDto
    {
        public string Text { get; set; } = "";
        public string FontName { get; set; }
        public float FontSize { get; set; }
        public bool Bold { get; set; }
        public bool Italic { get; set; }
        public bool Underline { get; set; }
        public bool Strikethrough { get; set; }
        public int TextColor { get; set; }
    }

    public class CollabSlideMetaDto
    {
        public string Id { get; set; } = "";
        public string Title { get; set; } = "";
        public int BgColor { get; set; }
        public bool IsGradientBg { get; set; }
        public int BgColor2 { get; set; }
        public float BgGradientAngle { get; set; }
        public int BgFillType { get; set; }
        public float BgTransparency { get; set; }
        public string SpeakerNotes { get; set; } = "";
        public bool IsHidden { get; set; }
        public int Transition { get; set; }
        public float TransitionDuration { get; set; }
    }

    public class CollabSlideOpPayload
    {
        public string Action { get; set; } = ""; // "shape_upsert", "shape_delete", "slide_upsert", "slide_delete", "slide_reorder"
        public string SlideId { get; set; } = "";
        public int SlideIndex { get; set; }
        public string ShapeId { get; set; } = "";
        public int ShapeOrderIndex { get; set; } = -1;
        public CollabShapeDto Shape { get; set; }
        public CollabSlideMetaDto SlideMeta { get; set; }
        public List<CollabShapeDto> SlideShapes { get; set; }
        public List<string> SlideOrderIds { get; set; }
    }

    public class CollabCalcCellDto
    {
        public int SheetIndex { get; set; }
        public uint Row { get; set; }
        public uint Col { get; set; }
        public bool IsEmpty { get; set; }
        public int Type { get; set; }
        public string RawValue { get; set; } = "";
        public string FormulaStr { get; set; } = "";
        public string FontName { get; set; } = "Calibri";
        public float FontSize { get; set; } = 11f;
        public bool Bold { get; set; }
        public bool Italic { get; set; }
        public bool Underline { get; set; }
        public bool Strikethrough { get; set; }
        public int TextColor { get; set; }
        public int BgColor { get; set; }
        public int Align { get; set; }
        public int VAlign { get; set; }
        public bool WrapText { get; set; }
        public string NumberFormat { get; set; } = "General";
    }

    public class CollabCalcPatchPayload
    {
        public List<CollabCalcCellDto> Cells { get; set; } = new List<CollabCalcCellDto>();
        public Dictionary<uint, int> ColWidths { get; set; }
        public Dictionary<uint, int> RowHeights { get; set; }
    }

    public class CollabWriterRunDto
    {
        public int StartIndex { get; set; }
        public int Length { get; set; }
        public string FontName { get; set; }
        public float FontSize { get; set; }
        public bool Bold { get; set; }
        public bool Italic { get; set; }
        public bool Underline { get; set; }
        public bool Strike { get; set; }
        public int ColorArgb { get; set; }
        public int HighlightArgb { get; set; }
        public bool Subscript { get; set; }
        public bool Superscript { get; set; }
    }

    public class CollabWriterPatchPayload
    {
        public int Offset { get; set; }
        public int DeleteLength { get; set; }
        public string DeletedText { get; set; } = "";
        public string InsertedText { get; set; } = "";
        public string ContextBefore { get; set; } = "";
        public string ContextAfter { get; set; } = "";
        public List<CollabWriterRunDto> Runs { get; set; }
        public Dictionary<int, int> ParagraphAlignments { get; set; }
        public Dictionary<int, string> ParagraphStyles { get; set; }
    }

    #endregion

    #region Per-User Undo Command Abstraction

    public class CollabUndoStep
    {
        public string Description { get; set; } = "";
        public List<CollabOpRecord> BeforeOps { get; set; } = new List<CollabOpRecord>();
        public List<CollabOpRecord> AfterOps { get; set; } = new List<CollabOpRecord>();
        // Writer-specific offset tracking for shifting A's pending Undo when B types earlier
        public int WriterOffset { get; set; }
        public int WriterOldLen { get; set; }
        public int WriterNewLen { get; set; }
        public string WriterOldText { get; set; } = "";
        public string WriterNewText { get; set; } = "";
        public List<CollabWriterRunDto> WriterOldRuns { get; set; }
        public List<CollabWriterRunDto> WriterNewRuns { get; set; }
        public Dictionary<int, int> WriterOldAligns { get; set; }
        public Dictionary<int, int> WriterNewAligns { get; set; }
    }

    #endregion

    /// <summary>
    /// Phien cong tac cho 1 tai lieu dang mo (Slides / Calc / Writer):
    /// - Luu Cloud tu dong moi 5 giay (chi luu khi co thay doi dirty).
    /// - Ctrl+Z / Ctrl+Y doc lap theo tung tai khoan nguoi dung (A Undo khong anh huong B).
    /// - Dong bo con tro, lua chon, di chuyen vat the thoi gian thực.
    /// </summary>
    public class DocCollabSession : IDisposable
    {
        public CollabRoomInfo Room { get; set; }
        public OfficeAppMode Mode => CloudShareService.ResolveDocMode(Room?.Title ?? "", Room?.DocMode ?? 3);
        public Guid RoomId => Room != null ? Room.Id : Guid.Empty;

        public bool IsDirtyForSnapshot { get; set; } = false;
        public bool IsSavingSnapshot { get; private set; } = false;
        public long LastOpSeqApplied { get; set; } = 0;
        public long LastSnapshotVersion { get; set; } = 1;
        public DateTime LastSavedUtc { get; set; } = DateTime.UtcNow;
        public string StatusLabel { get; set; } = "";

        public readonly ConcurrentDictionary<int, CollabPresenceInfo> RemotePresences
            = new ConcurrentDictionary<int, CollabPresenceInfo>();

        private readonly HashSet<Guid> _sentClientOpIds = new HashSet<Guid>();
        private readonly object _sentOpLock = new object();

        // Per-User isolated Undo & Redo stacks (ONLY this user's own operations!)
        private readonly Stack<CollabUndoStep> _localUndoStack = new Stack<CollabUndoStep>();
        private readonly Stack<CollabUndoStep> _localRedoStack = new Stack<CollabUndoStep>();
        private const int MaxCollabUndo = 80;

        public bool CanUndo => _localUndoStack.Count > 0;
        public bool CanRedo => _localRedoStack.Count > 0;

        // Throttling state for live transform & presence
        private long _lastLiveTransformSentTick = 0;
        private long _lastPresenceSentTick = 0;
        private int _lastSentSlideIdx = -1;
        private string _lastSentSelectedId = "";

        // Baseline states (kept in sync with both local & remote applied changes so diffs ONLY capture current user's edits)
        private List<(string SlideId, CollabSlideMetaDto Meta, List<(string ShapeId, CollabShapeDto Dto)> Shapes)> _slidesBaseline;
        private Dictionary<int, Dictionary<ulong, CollabCalcCellDto>> _calcBaseline;
        private string _writerBaselineText = "";
        private List<CollabWriterRunDto> _writerBaselineRuns = new List<CollabWriterRunDto>();
        private Dictionary<int, int> _writerBaselineAligns = new Dictionary<int, int>();

        private bool _isApplyingRemoteOrUndo = false;
        private bool _disposed = false;

        public event Action RequestUiRefresh;

        public DocCollabSession(CollabRoomInfo room, long initialOpSeq = 0)
        {
            Room = room;
            LastOpSeqApplied = initialOpSeq;
            LastSnapshotVersion = room != null ? room.SnapshotVersion : 1;
            StatusLabel = AppL10n.T("Đã kết nối Cloud", "Cloud synced");
        }

        public List<CollabPresenceInfo> GetActiveRemotePeers()
        {
            int myId = DatabaseAuthService.CurrentUser?.Id ?? -1;
            return RemotePresences.Values
                .Where(p => p != null && p.UserId != myId && !p.IsStale)
                .OrderBy(p => p.Username)
                .ToList();
        }

        #region Slides Baseline, Diff, Live Transform & Undo/Redo

        public static void EnsureSlideIds(PresentationRoot pres)
        {
            if (pres == null || pres.Slides == null) return;
            foreach (var slide in pres.Slides)
            {
                if (slide == null) continue;
                if (string.IsNullOrWhiteSpace(slide.Id))
                    slide.Id = Guid.NewGuid().ToString("N");
                if (slide.Shapes != null)
                {
                    foreach (var sh in slide.Shapes)
                    {
                        if (sh == null) continue;
                        if (string.IsNullOrWhiteSpace(sh.Id))
                            sh.Id = Guid.NewGuid().ToString("N");
                    }
                }
            }
        }

        public static CollabShapeDto ShapeToDto(ShapeNode sh)
        {
            if (sh == null) return null;
            var dto = new CollabShapeDto
            {
                Id = string.IsNullOrWhiteSpace(sh.Id) ? (sh.Id = Guid.NewGuid().ToString("N")) : sh.Id,
                PresetId = sh.PresetId,
                Name = sh.Name,
                Type = (int)sh.Type,
                X = sh.Transform?.X ?? 0f,
                Y = sh.Transform?.Y ?? 0f,
                Width = sh.Transform?.Width ?? 100f,
                Height = sh.Transform?.Height ?? 100f,
                Rotation = sh.Transform?.Rotation ?? 0f,
                ZIndex = sh.Transform?.ZIndex ?? 0,
                FlipHorizontal = sh.Transform?.FlipHorizontal ?? false,
                FlipVertical = sh.Transform?.FlipVertical ?? false,
                IsVisible = sh.IsVisible,
                Text = sh.Text ?? "",
                FontName = sh.FontName ?? "Segoe UI",
                FontSize = sh.FontSize,
                Bold = sh.Bold,
                Italic = sh.Italic,
                Underline = sh.Underline,
                Strikethrough = sh.Strikethrough,
                Alignment = (int)sh.Alignment,
                VerticalAlignment = (int)sh.VerticalAlignment,
                TextColor = sh.TextColor,
                LineSpacing = sh.LineSpacing,
                HasFill = sh.HasFill,
                FillColor = sh.FillColor,
                HasGradientFill = sh.HasGradientFill,
                GradientColor2 = sh.GradientColor2,
                GradientAngle = sh.GradientAngle,
                FillOpacity = sh.FillOpacity,
                StrokeColor = sh.StrokeColor,
                StrokeWidth = sh.StrokeWidth,
                StrokeOpacity = sh.StrokeOpacity,
                CornerRadius = sh.CornerRadius,
                ImagePath = sh.ImagePath ?? "",
                CropLeft = sh.CropLeft,
                CropTop = sh.CropTop,
                CropRight = sh.CropRight,
                CropBottom = sh.CropBottom,
                TableRows = sh.TableRows,
                TableColumns = sh.TableColumns,
                TableData = sh.TableData != null ? sh.TableData.Select(r => r != null ? new List<string>(r) : new List<string>()).ToList() : null
            };
            if (sh.TextRuns != null && sh.TextRuns.Count > 0)
            {
                dto.TextRuns = sh.TextRuns.Select(r => new CollabSlideRunDto
                {
                    Text = r.Text ?? "",
                    FontName = r.FontName,
                    FontSize = r.FontSize,
                    Bold = r.Bold,
                    Italic = r.Italic,
                    Underline = r.Underline,
                    Strikethrough = r.Strikethrough,
                    TextColor = r.TextColor
                }).ToList();
            }
            return dto;
        }

        public static ShapeNode DtoToShape(CollabShapeDto dto, ShapeNode existing = null)
        {
            if (dto == null) return null;
            ShapeNode sh = existing ?? new ShapeNode();
            sh.Id = dto.Id;
            sh.PresetId = dto.PresetId;
            sh.Name = dto.Name;
            sh.Type = (ShapeType)dto.Type;
            if (sh.Transform == null) sh.Transform = new Transform2D();
            sh.Transform.X = dto.X;
            sh.Transform.Y = dto.Y;
            sh.Transform.Width = dto.Width;
            sh.Transform.Height = dto.Height;
            sh.Transform.Rotation = dto.Rotation;
            sh.Transform.ZIndex = dto.ZIndex;
            sh.Transform.FlipHorizontal = dto.FlipHorizontal;
            sh.Transform.FlipVertical = dto.FlipVertical;
            sh.IsVisible = dto.IsVisible;
            sh.Text = dto.Text ?? "";
            sh.FontName = dto.FontName ?? "Segoe UI";
            sh.FontSize = dto.FontSize > 0 ? dto.FontSize : 14f;
            sh.Bold = dto.Bold;
            sh.Italic = dto.Italic;
            sh.Underline = dto.Underline;
            sh.Strikethrough = dto.Strikethrough;
            sh.Alignment = (TextAlignment)dto.Alignment;
            sh.VerticalAlignment = (VerticalTextAlignment)dto.VerticalAlignment;
            sh.TextColor = dto.TextColor;
            sh.LineSpacing = dto.LineSpacing;
            sh.HasFill = dto.HasFill;
            sh.FillColor = dto.FillColor;
            sh.HasGradientFill = dto.HasGradientFill;
            sh.GradientColor2 = dto.GradientColor2;
            sh.GradientAngle = dto.GradientAngle;
            sh.FillOpacity = dto.FillOpacity;
            sh.StrokeColor = dto.StrokeColor;
            sh.StrokeWidth = dto.StrokeWidth;
            sh.StrokeOpacity = dto.StrokeOpacity;
            sh.CornerRadius = dto.CornerRadius;
            sh.CropLeft = dto.CropLeft;
            sh.CropTop = dto.CropTop;
            sh.CropRight = dto.CropRight;
            sh.CropBottom = dto.CropBottom;
            sh.TableRows = dto.TableRows;
            sh.TableColumns = dto.TableColumns;
            if (dto.TableData != null)
                sh.TableData = dto.TableData.Select(r => r != null ? new List<string>(r) : new List<string>()).ToList();

            if (!string.IsNullOrEmpty(dto.ImagePath) && File.Exists(dto.ImagePath))
            {
                if (sh.ImagePath != dto.ImagePath)
                {
                    sh.ImagePath = dto.ImagePath;
                    try { sh.CachedImage = Image.FromFile(dto.ImagePath); sh.InvalidateDisplayCache(); } catch { }
                }
            }

            if (dto.TextRuns != null)
            {
                sh.TextRuns = dto.TextRuns.Select(r => new SlideTextRun
                {
                    Text = r.Text ?? "",
                    FontName = r.FontName ?? sh.FontName,
                    FontSize = r.FontSize > 0 ? r.FontSize : sh.FontSize,
                    Bold = r.Bold,
                    Italic = r.Italic,
                    Underline = r.Underline,
                    Strikethrough = r.Strikethrough,
                    TextColor = r.TextColor
                }).ToList();
            }
            sh.InvalidateVisualLines();
            sh.InvalidateShadowCache();
            return sh;
        }

        private static CollabSlideMetaDto SlideToMetaDto(SlideNode s)
        {
            return new CollabSlideMetaDto
            {
                Id = s.Id ?? "",
                Title = s.Title ?? "",
                BgColor = s.BgColor,
                IsGradientBg = s.IsGradientBg,
                BgColor2 = s.BgColor2,
                BgGradientAngle = s.BgGradientAngle,
                BgFillType = (int)s.BgFillType,
                BgTransparency = s.BgTransparency,
                SpeakerNotes = s.SpeakerNotes ?? "",
                IsHidden = s.IsHidden,
                Transition = (int)s.Transition,
                TransitionDuration = s.TransitionDuration
            };
        }

        private static void ApplyMetaDto(SlideNode s, CollabSlideMetaDto m)
        {
            if (s == null || m == null) return;
            s.Title = m.Title ?? "";
            s.BgColor = m.BgColor;
            s.IsGradientBg = m.IsGradientBg;
            s.BgColor2 = m.BgColor2;
            s.BgGradientAngle = m.BgGradientAngle;
            s.BgFillType = (BackgroundFillType)m.BgFillType;
            s.BgTransparency = m.BgTransparency;
            s.SpeakerNotes = m.SpeakerNotes ?? "";
            s.IsHidden = m.IsHidden;
            s.Transition = (TransitionType)m.Transition;
            s.TransitionDuration = m.TransitionDuration;
        }

        private List<(string SlideId, CollabSlideMetaDto Meta, List<(string ShapeId, CollabShapeDto Dto)> Shapes)> CaptureSlidesState(PresentationRoot pres)
        {
            var res = new List<(string, CollabSlideMetaDto, List<(string, CollabShapeDto)>)>();
            if (pres == null || pres.Slides == null) return res;
            EnsureSlideIds(pres);
            foreach (var slide in pres.Slides)
            {
                if (slide == null) continue;
                var shapes = new List<(string, CollabShapeDto)>();
                if (slide.Shapes != null)
                {
                    foreach (var sh in slide.Shapes)
                    {
                        if (sh == null || (sh.Name != null && sh.Name.StartsWith("__morph_ghost__"))) continue;
                        shapes.Add((sh.Id, ShapeToDto(sh)));
                    }
                }
                res.Add((slide.Id, SlideToMetaDto(slide), shapes));
            }
            return res;
        }

        public void InitializeSlidesBaseline(SlidesView view)
        {
            if (view?.Presentation == null) return;
            _slidesBaseline = CaptureSlidesState(view.Presentation);
        }

        public void NotifySlideLiveTransform(SlidesView view)
        {
            if (_isApplyingRemoteOrUndo || Room == null || view?.ActiveSlide == null || view.SelectedShape == null) return;
            long now = Environment.TickCount64;
            if (now - _lastLiveTransformSentTick < 65) return; // ~15fps live stream via pg_notify
            _lastLiveTransformSentTick = now;

            var slide = view.ActiveSlide;
            var shape = view.SelectedShape;
            if (string.IsNullOrWhiteSpace(slide.Id)) slide.Id = Guid.NewGuid().ToString("N");
            if (string.IsNullOrWhiteSpace(shape.Id)) shape.Id = Guid.NewGuid().ToString("N");

            if (view.TryScreenToSlideLogical(view.CurrentMouseCanvasPos, out float lx, out float ly))
            {
                // Keep CursorX/Y in logical slide units so remote views at any zoom render cursor accurately
                var sig = new CollabLiveSignal
                {
                    Type = "live_transform",
                    SlideId = slide.Id,
                    ShapeId = shape.Id,
                    SlideIndex = view.ActiveSlideIndex,
                    SelectedId = shape.Id,
                    X = shape.Transform?.X ?? 0f,
                    Y = shape.Transform?.Y ?? 0f,
                    W = shape.Transform?.Width ?? 0f,
                    H = shape.Transform?.Height ?? 0f,
                    Rotation = shape.Transform?.Rotation ?? 0f,
                    CursorX = lx,
                    CursorY = ly
                };
                _ = CollabRoomService.PushLiveSignalAsync(RoomId, sig);
                return;
            }

            var sigFallback = new CollabLiveSignal
            {
                Type = "live_transform",
                SlideId = slide.Id,
                ShapeId = shape.Id,
                SlideIndex = view.ActiveSlideIndex,
                SelectedId = shape.Id,
                X = shape.Transform?.X ?? 0f,
                Y = shape.Transform?.Y ?? 0f,
                W = shape.Transform?.Width ?? 0f,
                H = shape.Transform?.Height ?? 0f,
                Rotation = shape.Transform?.Rotation ?? 0f,
                CursorX = view.CurrentMouseCanvasPos.X,
                CursorY = view.CurrentMouseCanvasPos.Y
            };
            _ = CollabRoomService.PushLiveSignalAsync(RoomId, sigFallback);
        }

        public void CaptureAndPushSlideEdit(SlidesView view, string description = "Slide edit")
        {
            if (_isApplyingRemoteOrUndo || Room == null || view?.Presentation == null) return;
            var current = CaptureSlidesState(view.Presentation);
            if (_slidesBaseline == null)
            {
                _slidesBaseline = current;
                return;
            }

            var beforeOps = new List<CollabOpRecord>();
            var afterOps = new List<CollabOpRecord>();

            var oldSlideMap = _slidesBaseline.ToDictionary(x => x.SlideId, x => x);
            var newSlideMap = current.ToDictionary(x => x.SlideId, x => x);

            // Detect deleted slides
            for (int i = 0; i < _slidesBaseline.Count; i++)
            {
                var oldS = _slidesBaseline[i];
                if (!newSlideMap.ContainsKey(oldS.SlideId))
                {
                    var restorePayload = new CollabSlideOpPayload
                    {
                        Action = "slide_upsert",
                        SlideId = oldS.SlideId,
                        SlideIndex = i,
                        SlideMeta = oldS.Meta,
                        SlideShapes = oldS.Shapes.Select(s => s.Dto).ToList()
                    };
                    var delPayload = new CollabSlideOpPayload { Action = "slide_delete", SlideId = oldS.SlideId };
                    beforeOps.Add(MakeOp("slide_op", oldS.SlideId, restorePayload));
                    afterOps.Add(MakeOp("slide_op", oldS.SlideId, delPayload));
                }
            }

            // Detect added slides or modified slides/shapes
            for (int si = 0; si < current.Count; si++)
            {
                var newS = current[si];
                if (!oldSlideMap.TryGetValue(newS.SlideId, out var oldS))
                {
                    var addPayload = new CollabSlideOpPayload
                    {
                        Action = "slide_upsert",
                        SlideId = newS.SlideId,
                        SlideIndex = si,
                        SlideMeta = newS.Meta,
                        SlideShapes = newS.Shapes.Select(s => s.Dto).ToList()
                    };
                    var undoDel = new CollabSlideOpPayload { Action = "slide_delete", SlideId = newS.SlideId };
                    beforeOps.Add(MakeOp("slide_op", newS.SlideId, undoDel));
                    afterOps.Add(MakeOp("slide_op", newS.SlideId, addPayload));
                    continue;
                }

                // Compare slide metadata
                string oldMetaJson = JsonSerializer.Serialize(oldS.Meta);
                string newMetaJson = JsonSerializer.Serialize(newS.Meta);
                if (oldMetaJson != newMetaJson)
                {
                    beforeOps.Add(MakeOp("slide_op", newS.SlideId, new CollabSlideOpPayload { Action = "slide_meta", SlideId = newS.SlideId, SlideMeta = oldS.Meta }));
                    afterOps.Add(MakeOp("slide_op", newS.SlideId, new CollabSlideOpPayload { Action = "slide_meta", SlideId = newS.SlideId, SlideMeta = newS.Meta }));
                }

                // Compare shapes in slide
                var oldShapeMap = oldS.Shapes.ToDictionary(s => s.ShapeId, s => s);
                var newShapeMap = newS.Shapes.ToDictionary(s => s.ShapeId, s => s);

                foreach (var os in oldS.Shapes)
                {
                    if (!newShapeMap.ContainsKey(os.ShapeId))
                    {
                        int oldOrd = oldS.Shapes.FindIndex(x => x.ShapeId == os.ShapeId);
                        beforeOps.Add(MakeOp("slide_op", $"{newS.SlideId}:{os.ShapeId}", new CollabSlideOpPayload
                        {
                            Action = "shape_upsert",
                            SlideId = newS.SlideId,
                            ShapeId = os.ShapeId,
                            ShapeOrderIndex = oldOrd,
                            Shape = os.Dto
                        }));
                        afterOps.Add(MakeOp("slide_op", $"{newS.SlideId}:{os.ShapeId}", new CollabSlideOpPayload
                        {
                            Action = "shape_delete",
                            SlideId = newS.SlideId,
                            ShapeId = os.ShapeId
                        }));
                    }
                }

                for (int shi = 0; shi < newS.Shapes.Count; shi++)
                {
                    var ns = newS.Shapes[shi];
                    if (!oldShapeMap.TryGetValue(ns.ShapeId, out var os))
                    {
                        // New shape added
                        EnsureUploadShapeAssetAsync(ns.Dto);
                        beforeOps.Add(MakeOp("slide_op", $"{newS.SlideId}:{ns.ShapeId}", new CollabSlideOpPayload
                        {
                            Action = "shape_delete",
                            SlideId = newS.SlideId,
                            ShapeId = ns.ShapeId
                        }));
                        afterOps.Add(MakeOp("slide_op", $"{newS.SlideId}:{ns.ShapeId}", new CollabSlideOpPayload
                        {
                            Action = "shape_upsert",
                            SlideId = newS.SlideId,
                            ShapeId = ns.ShapeId,
                            ShapeOrderIndex = shi,
                            Shape = ns.Dto
                        }));
                    }
                    else
                    {
                        string oj = JsonSerializer.Serialize(os.Dto);
                        string nj = JsonSerializer.Serialize(ns.Dto);
                        if (oj != nj)
                        {
                            if (ns.Dto.ImagePath != os.Dto.ImagePath && !string.IsNullOrEmpty(ns.Dto.ImagePath))
                                EnsureUploadShapeAssetAsync(ns.Dto);

                            int oldOrd = oldS.Shapes.FindIndex(x => x.ShapeId == os.ShapeId);
                            beforeOps.Add(MakeOp("slide_op", $"{newS.SlideId}:{os.ShapeId}", new CollabSlideOpPayload
                            {
                                Action = "shape_upsert",
                                SlideId = newS.SlideId,
                                ShapeId = os.ShapeId,
                                ShapeOrderIndex = oldOrd,
                                Shape = os.Dto
                            }));
                            afterOps.Add(MakeOp("slide_op", $"{newS.SlideId}:{ns.ShapeId}", new CollabSlideOpPayload
                            {
                                Action = "shape_upsert",
                                SlideId = newS.SlideId,
                                ShapeId = ns.ShapeId,
                                ShapeOrderIndex = shi,
                                Shape = ns.Dto
                            }));
                        }
                    }
                }
            }

            // Detect slide order change when counts match
            if (_slidesBaseline.Count == current.Count)
            {
                bool orderDiff = false;
                for (int i = 0; i < current.Count; i++)
                {
                    if (_slidesBaseline[i].SlideId != current[i].SlideId) { orderDiff = true; break; }
                }
                if (orderDiff && beforeOps.Count == 0)
                {
                    beforeOps.Add(MakeOp("slide_op", "order", new CollabSlideOpPayload
                    {
                        Action = "slide_reorder",
                        SlideOrderIds = _slidesBaseline.Select(x => x.SlideId).ToList()
                    }));
                    afterOps.Add(MakeOp("slide_op", "order", new CollabSlideOpPayload
                    {
                        Action = "slide_reorder",
                        SlideOrderIds = current.Select(x => x.SlideId).ToList()
                    }));
                }
            }

            _slidesBaseline = current;
            if (afterOps.Count == 0) return;

            IsDirtyForSnapshot = true;
            PushLocalUndoStep(new CollabUndoStep
            {
                Description = description,
                BeforeOps = beforeOps,
                AfterOps = afterOps
            });
            BroadcastOpsAsync(afterOps);
        }

        private void EnsureUploadShapeAssetAsync(CollabShapeDto dto)
        {
            if (dto == null || string.IsNullOrEmpty(dto.ImagePath) || !File.Exists(dto.ImagePath)) return;
            try
            {
                // Synchronous small wait or background hash so AssetKey is embedded quickly
                string key = Task.Run(() => CollabRoomService.EnsureAssetUploadedAsync(RoomId, dto.ImagePath)).GetAwaiter().GetResult();
                if (!string.IsNullOrEmpty(key))
                    dto.ImageAssetKey = key;
            }
            catch { }
        }

        #endregion

        #region Calc Baseline, Diff & Undo/Redo

        private static CollabCalcCellDto CellToDto(int sheetIdx, uint row, uint col, CellData cell)
        {
            if (cell == null || cell.Type == CellType.Empty && string.IsNullOrEmpty(cell.RawValue) && cell.Format == null)
            {
                return new CollabCalcCellDto { SheetIndex = sheetIdx, Row = row, Col = col, IsEmpty = true };
            }
            var fmt = cell.Format ?? new CellFormat();
            return new CollabCalcCellDto
            {
                SheetIndex = sheetIdx,
                Row = row,
                Col = col,
                IsEmpty = false,
                Type = (int)cell.Type,
                RawValue = cell.RawValue ?? "",
                FormulaStr = cell.FormulaStr ?? "",
                FontName = fmt.FontName ?? "Calibri",
                FontSize = fmt.FontSize > 0 ? fmt.FontSize : 11f,
                Bold = fmt.Bold,
                Italic = fmt.Italic,
                Underline = fmt.Underline,
                Strikethrough = fmt.Strikethrough,
                TextColor = fmt.TextColor,
                BgColor = fmt.BgColor,
                Align = fmt.Align,
                VAlign = fmt.VAlign,
                WrapText = fmt.WrapText,
                NumberFormat = fmt.NumberFormat ?? "General"
            };
        }

        private static void ApplyCellDtoToSheet(SparseMatrix sheet, CollabCalcCellDto dto)
        {
            if (sheet == null || dto == null) return;
            if (dto.IsEmpty)
            {
                sheet.ClearCell(dto.Row, dto.Col);
                return;
            }
            var cell = sheet.GetOrCreateCell(dto.Row, dto.Col);
            cell.Type = (CellType)dto.Type;
            cell.RawValue = dto.RawValue ?? "";
            cell.FormulaStr = dto.FormulaStr ?? "";
            if (!string.IsNullOrEmpty(cell.FormulaStr))
            {
                cell.SetFormula(cell.FormulaStr);
            }
            else if (cell.Type == CellType.Number && double.TryParse(cell.RawValue, out double num))
            {
                cell.SetNumber(num);
            }
            else
            {
                cell.SetString(cell.RawValue);
            }
            if (cell.Format == null) cell.Format = new CellFormat();
            cell.Format.FontName = dto.FontName ?? "Calibri";
            cell.Format.FontSize = dto.FontSize > 0 ? dto.FontSize : 11f;
            cell.Format.Bold = dto.Bold;
            cell.Format.Italic = dto.Italic;
            cell.Format.Underline = dto.Underline;
            cell.Format.Strikethrough = dto.Strikethrough;
            cell.Format.TextColor = dto.TextColor;
            cell.Format.BgColor = dto.BgColor;
            cell.Format.Align = dto.Align;
            cell.Format.VAlign = dto.VAlign;
            cell.Format.WrapText = dto.WrapText;
            cell.Format.NumberFormat = dto.NumberFormat ?? "General";
        }

        private Dictionary<int, Dictionary<ulong, CollabCalcCellDto>> CaptureCalcState(CalcView view)
        {
            var res = new Dictionary<int, Dictionary<ulong, CollabCalcCellDto>>();
            if (view?.Sheets == null) return res;
            for (int si = 0; si < view.Sheets.Count; si++)
            {
                var sheet = view.Sheets[si];
                if (sheet == null) continue;
                var map = new Dictionary<ulong, CollabCalcCellDto>();
                var cloned = sheet.CloneGrid();
                foreach (var kv in cloned)
                {
                    var rc = SparseMatrix.DecodeKey(kv.Key);
                    map[kv.Key] = CellToDto(si, rc.Item1, rc.Item2, kv.Value);
                }
                res[si] = map;
            }
            return res;
        }

        public void InitializeCalcBaseline(CalcView view)
        {
            if (view == null) return;
            _calcBaseline = CaptureCalcState(view);
        }

        public void CaptureAndPushCalcEdit(CalcView view, string description = "Cell edit")
        {
            if (_isApplyingRemoteOrUndo || Room == null || view == null) return;
            var current = CaptureCalcState(view);
            if (_calcBaseline == null)
            {
                _calcBaseline = current;
                return;
            }

            var beforeCells = new List<CollabCalcCellDto>();
            var afterCells = new List<CollabCalcCellDto>();

            var allSheets = _calcBaseline.Keys.Union(current.Keys).Distinct();
            foreach (int si in allSheets)
            {
                _calcBaseline.TryGetValue(si, out var oldMap);
                current.TryGetValue(si, out var newMap);
                oldMap ??= new Dictionary<ulong, CollabCalcCellDto>();
                newMap ??= new Dictionary<ulong, CollabCalcCellDto>();

                foreach (var key in oldMap.Keys.Union(newMap.Keys))
                {
                    oldMap.TryGetValue(key, out var oCell);
                    newMap.TryGetValue(key, out var nCell);
                    string oj = oCell != null ? JsonSerializer.Serialize(oCell) : "";
                    string nj = nCell != null ? JsonSerializer.Serialize(nCell) : "";
                    if (oj != nj)
                    {
                        var rc = SparseMatrix.DecodeKey(key);
                        beforeCells.Add(oCell ?? new CollabCalcCellDto { SheetIndex = si, Row = rc.Item1, Col = rc.Item2, IsEmpty = true });
                        afterCells.Add(nCell ?? new CollabCalcCellDto { SheetIndex = si, Row = rc.Item1, Col = rc.Item2, IsEmpty = true });
                    }
                }
            }

            _calcBaseline = current;
            if (afterCells.Count == 0) return;

            var bOp = MakeOp("calc_patch", $"s{view.ActiveSheetIndex}", new CollabCalcPatchPayload { Cells = beforeCells });
            var aOp = MakeOp("calc_patch", $"s{view.ActiveSheetIndex}", new CollabCalcPatchPayload { Cells = afterCells });

            IsDirtyForSnapshot = true;
            PushLocalUndoStep(new CollabUndoStep
            {
                Description = description,
                BeforeOps = new List<CollabOpRecord> { bOp },
                AfterOps = new List<CollabOpRecord> { aOp }
            });
            BroadcastOpsAsync(new List<CollabOpRecord> { aOp });
        }

        #endregion

        #region Writer Baseline, Minimal Diff, Anchor Merge & Per-User Undo/Redo

        private static List<CollabWriterRunDto> CaptureWriterRuns(WriterView view)
        {
            var list = new List<CollabWriterRunDto>();
            if (view?.FormattedRuns == null) return list;
            foreach (var r in view.FormattedRuns)
            {
                if (r == null) continue;
                list.Add(new CollabWriterRunDto
                {
                    StartIndex = r.StartIndex,
                    Length = r.Length,
                    FontName = r.FontName ?? "Segoe UI",
                    FontSize = r.FontSize,
                    Bold = r.Bold,
                    Italic = r.Italic,
                    Underline = r.Underline,
                    Strike = r.Strike,
                    ColorArgb = r.Color.ToArgb(),
                    HighlightArgb = r.Highlight.ToArgb(),
                    Subscript = r.Subscript,
                    Superscript = r.Superscript
                });
            }
            return list;
        }

        private static void ApplyWriterRuns(WriterView view, List<CollabWriterRunDto> dtos)
        {
            if (view == null || dtos == null) return;
            var newRuns = new List<FormattedRun>(dtos.Count);
            foreach (var d in dtos)
            {
                newRuns.Add(new FormattedRun(
                    d.StartIndex, d.Length,
                    d.FontName ?? "Segoe UI", d.FontSize > 0 ? d.FontSize : 11f,
                    d.Bold, d.Italic, d.Underline, d.Strike,
                    Color.FromArgb(d.ColorArgb),
                    Color.FromArgb(d.HighlightArgb),
                    d.Subscript, d.Superscript));
            }
            view.FormattedRuns = newRuns;
            view.SyncCurrentTabRuns();
        }

        public void InitializeWriterBaseline(WriterView view)
        {
            if (view == null) return;
            _writerBaselineText = view.PieceTable != null ? (view.PieceTable.GetText() ?? "") : "";
            _writerBaselineRuns = CaptureWriterRuns(view);
            _writerBaselineAligns = view.ParagraphAlignments != null
                ? new Dictionary<int, int>(view.ParagraphAlignments)
                : new Dictionary<int, int>();
        }

        public void CaptureAndPushWriterEdit(WriterView view, string description = "Text edit")
        {
            if (_isApplyingRemoteOrUndo || Room == null || view?.PieceTable == null) return;
            string curText = view.PieceTable.GetText() ?? "";
            var curRuns = CaptureWriterRuns(view);
            var curAligns = view.ParagraphAlignments != null ? new Dictionary<int, int>(view.ParagraphAlignments) : new Dictionary<int, int>();

            string oldText = _writerBaselineText ?? "";
            int prefix = 0;
            int minLen = Math.Min(oldText.Length, curText.Length);
            while (prefix < minLen && oldText[prefix] == curText[prefix]) prefix++;

            int oldEnd = oldText.Length;
            int newEnd = curText.Length;
            while (oldEnd > prefix && newEnd > prefix && oldText[oldEnd - 1] == curText[newEnd - 1])
            {
                oldEnd--;
                newEnd--;
            }

            string deleted = oldText.Substring(prefix, Math.Max(0, oldEnd - prefix));
            string inserted = curText.Substring(prefix, Math.Max(0, newEnd - prefix));

            bool runsChanged = JsonSerializer.Serialize(_writerBaselineRuns) != JsonSerializer.Serialize(curRuns);
            bool alignsChanged = JsonSerializer.Serialize(_writerBaselineAligns) != JsonSerializer.Serialize(curAligns);

            if (string.IsNullOrEmpty(deleted) && string.IsNullOrEmpty(inserted) && !runsChanged && !alignsChanged)
                return;

            string ctxBefore = prefix > 0 ? oldText.Substring(Math.Max(0, prefix - 16), Math.Min(16, prefix)) : "";
            string ctxAfter = oldEnd < oldText.Length ? oldText.Substring(oldEnd, Math.Min(16, oldText.Length - oldEnd)) : "";

            var afterPayload = new CollabWriterPatchPayload
            {
                Offset = prefix,
                DeleteLength = deleted.Length,
                DeletedText = deleted,
                InsertedText = inserted,
                ContextBefore = ctxBefore,
                ContextAfter = ctxAfter,
                Runs = curRuns,
                ParagraphAlignments = curAligns
            };
            var beforePayload = new CollabWriterPatchPayload
            {
                Offset = prefix,
                DeleteLength = inserted.Length,
                DeletedText = inserted,
                InsertedText = deleted,
                ContextBefore = ctxBefore,
                ContextAfter = ctxAfter,
                Runs = _writerBaselineRuns,
                ParagraphAlignments = _writerBaselineAligns
            };

            var step = new CollabUndoStep
            {
                Description = description,
                BeforeOps = new List<CollabOpRecord> { MakeOp("writer_patch", $"p{prefix}", beforePayload) },
                AfterOps = new List<CollabOpRecord> { MakeOp("writer_patch", $"p{prefix}", afterPayload) },
                WriterOffset = prefix,
                WriterOldLen = deleted.Length,
                WriterNewLen = inserted.Length,
                WriterOldText = deleted,
                WriterNewText = inserted,
                WriterOldRuns = _writerBaselineRuns,
                WriterNewRuns = curRuns,
                WriterOldAligns = _writerBaselineAligns,
                WriterNewAligns = curAligns
            };

            _writerBaselineText = curText;
            _writerBaselineRuns = curRuns;
            _writerBaselineAligns = curAligns;
            IsDirtyForSnapshot = true;

            PushLocalUndoStep(step);
            BroadcastOpsAsync(step.AfterOps);
        }

        private static string SpliceString(string source, int offset, int deleteLen, string insertText)
        {
            source ??= "";
            int safeOff = Math.Max(0, Math.Min(source.Length, offset));
            int safeDel = Math.Max(0, Math.Min(source.Length - safeOff, deleteLen));
            var sb = new StringBuilder(source.Length - safeDel + (insertText?.Length ?? 0));
            if (safeOff > 0) sb.Append(source, 0, safeOff);
            if (!string.IsNullOrEmpty(insertText)) sb.Append(insertText);
            if (safeOff + safeDel < source.Length) sb.Append(source, safeOff + safeDel, source.Length - safeOff - safeDel);
            return sb.ToString();
        }

        private static int ResolveAnchoredOffset(string currentText, CollabWriterPatchPayload p)
        {
            if (string.IsNullOrEmpty(currentText)) return 0;
            int off = Math.Max(0, Math.Min(currentText.Length, p.Offset));

            // Exact match at expected offset
            if (!string.IsNullOrEmpty(p.DeletedText))
            {
                if (off + p.DeletedText.Length <= currentText.Length &&
                    string.Compare(currentText, off, p.DeletedText, 0, p.DeletedText.Length, StringComparison.Ordinal) == 0)
                {
                    return off;
                }
            }
            else if (!string.IsNullOrEmpty(p.ContextBefore) && off >= p.ContextBefore.Length &&
                     string.Compare(currentText, off - p.ContextBefore.Length, p.ContextBefore, 0, p.ContextBefore.Length, StringComparison.Ordinal) == 0)
            {
                return off;
            }

            // Search nearby anchored context within +/- 512 chars
            if (!string.IsNullOrEmpty(p.ContextBefore))
            {
                int searchStart = Math.Max(0, off - 512);
                int idx = currentText.IndexOf(p.ContextBefore, searchStart, StringComparison.Ordinal);
                if (idx >= 0) return Math.Min(currentText.Length, idx + p.ContextBefore.Length);
            }
            return off;
        }

        private void ShiftPendingWriterUndoOffsets(int editOffset, int delta)
        {
            if (delta == 0) return;
            foreach (var st in _localUndoStack)
            {
                if (st.WriterOffset > editOffset)
                    st.WriterOffset = Math.Max(0, st.WriterOffset + delta);
            }
            foreach (var st in _localRedoStack)
            {
                if (st.WriterOffset > editOffset)
                    st.WriterOffset = Math.Max(0, st.WriterOffset + delta);
            }
        }

        #endregion

        #region Local Per-User Undo & Redo (Strictly Does NOT Touch Remote Edits)

        private void PushLocalUndoStep(CollabUndoStep step)
        {
            if (step == null) return;
            // Coalesce quick single-character Writer typing within 450ms for smooth Ctrl+Z word/burst feel
            if (_localUndoStack.Count > 0 && Mode == OfficeAppMode.Writer &&
                string.IsNullOrEmpty(step.WriterOldText) && step.WriterNewText.Length == 1 && !char.IsWhiteSpace(step.WriterNewText[0]))
            {
                var top = _localUndoStack.Peek();
                if (string.IsNullOrEmpty(top.WriterOldText) &&
                    top.WriterNewText.Length > 0 && top.WriterNewText.Length < 40 &&
                    top.WriterOffset + top.WriterNewLen == step.WriterOffset &&
                    step.AfterOps.Count == 1 && top.AfterOps.Count == 1 &&
                    (DateTime.UtcNow - top.AfterOps[0].CreatedAt).TotalMilliseconds < 450)
                {
                    top.WriterNewText += step.WriterNewText;
                    top.WriterNewLen += step.WriterNewLen;
                    top.WriterNewRuns = step.WriterNewRuns;
                    top.WriterNewAligns = step.WriterNewAligns;
                    top.AfterOps = step.AfterOps;
                    top.AfterOps[0].CreatedAt = DateTime.UtcNow;
                    _localRedoStack.Clear();
                    return;
                }
            }

            if (_localUndoStack.Count >= MaxCollabUndo)
            {
                var arr = _localUndoStack.ToArray();
                _localUndoStack.Clear();
                for (int i = arr.Length - 2; i >= 0; i--)
                    _localUndoStack.Push(arr[i]);
            }
            _localUndoStack.Push(step);
            _localRedoStack.Clear();
        }

        public bool Undo(SlidesView slides, CalcView calc, WriterView writer, RibbonBar ribbon)
        {
            if (_localUndoStack.Count == 0) return false;
            var step = _localUndoStack.Pop();
            _localRedoStack.Push(step);

            _isApplyingRemoteOrUndo = true;
            try
            {
                if (Mode == OfficeAppMode.Writer && writer?.PieceTable != null)
                {
                    string cur = writer.PieceTable.GetText() ?? "";
                    int off = Math.Max(0, Math.Min(cur.Length, step.WriterOffset));
                    int delLen = Math.Min(cur.Length - off, step.WriterNewLen);
                    string next = SpliceString(cur, off, delLen, step.WriterOldText);
                    writer.PieceTable.SetInitialText(next);
                    if (step.WriterOldRuns != null) ApplyWriterRuns(writer, step.WriterOldRuns);
                    if (step.WriterOldAligns != null) writer.ParagraphAlignments = new Dictionary<int, int>(step.WriterOldAligns);
                    writer.CursorPosition = Math.Max(0, Math.Min(next.Length, off + step.WriterOldLen));
                    writer.SelectionStart = writer.CursorPosition;
                    writer.SelectionEnd = writer.CursorPosition;

                    var invPayload = new CollabWriterPatchPayload
                    {
                        Offset = off,
                        DeleteLength = delLen,
                        DeletedText = step.WriterNewText,
                        InsertedText = step.WriterOldText,
                        Runs = step.WriterOldRuns,
                        ParagraphAlignments = step.WriterOldAligns
                    };
                    InitializeWriterBaseline(writer);
                    IsDirtyForSnapshot = true;
                    BroadcastOpsAsync(new List<CollabOpRecord> { MakeOp("writer_patch", $"u{off}", invPayload) });
                    return true;
                }

                ApplyOpsLocally(step.BeforeOps, slides, calc, writer, ribbon);
                IsDirtyForSnapshot = true;
                var opsToSend = step.BeforeOps.Select(CloneWithFreshClientOpId).ToList();
                BroadcastOpsAsync(opsToSend);
                return true;
            }
            finally
            {
                _isApplyingRemoteOrUndo = false;
            }
        }

        public bool Redo(SlidesView slides, CalcView calc, WriterView writer, RibbonBar ribbon)
        {
            if (_localRedoStack.Count == 0) return false;
            var step = _localRedoStack.Pop();
            _localUndoStack.Push(step);

            _isApplyingRemoteOrUndo = true;
            try
            {
                if (Mode == OfficeAppMode.Writer && writer?.PieceTable != null)
                {
                    string cur = writer.PieceTable.GetText() ?? "";
                    int off = Math.Max(0, Math.Min(cur.Length, step.WriterOffset));
                    int delLen = Math.Min(cur.Length - off, step.WriterOldLen);
                    string next = SpliceString(cur, off, delLen, step.WriterNewText);
                    writer.PieceTable.SetInitialText(next);
                    if (step.WriterNewRuns != null) ApplyWriterRuns(writer, step.WriterNewRuns);
                    if (step.WriterNewAligns != null) writer.ParagraphAlignments = new Dictionary<int, int>(step.WriterNewAligns);
                    writer.CursorPosition = Math.Max(0, Math.Min(next.Length, off + step.WriterNewLen));
                    writer.SelectionStart = writer.CursorPosition;
                    writer.SelectionEnd = writer.CursorPosition;

                    var redoPayload = new CollabWriterPatchPayload
                    {
                        Offset = off,
                        DeleteLength = delLen,
                        DeletedText = step.WriterOldText,
                        InsertedText = step.WriterNewText,
                        Runs = step.WriterNewRuns,
                        ParagraphAlignments = step.WriterNewAligns
                    };
                    InitializeWriterBaseline(writer);
                    IsDirtyForSnapshot = true;
                    BroadcastOpsAsync(new List<CollabOpRecord> { MakeOp("writer_patch", $"r{off}", redoPayload) });
                    return true;
                }

                ApplyOpsLocally(step.AfterOps, slides, calc, writer, ribbon);
                IsDirtyForSnapshot = true;
                var opsToSend = step.AfterOps.Select(CloneWithFreshClientOpId).ToList();
                BroadcastOpsAsync(opsToSend);
                return true;
            }
            finally
            {
                _isApplyingRemoteOrUndo = false;
            }
        }

        #endregion

        #region Remote Ops & Live Signal Application

        public void ApplyRemoteLiveSignal(CollabLiveSignal sig, SlidesView slides)
        {
            if (sig == null || sig.RoomId != RoomId) return;
            int myId = DatabaseAuthService.CurrentUser?.Id ?? -1;
            if (sig.UserId == myId) return;

            if (sig.Type == "presence" || sig.Type == "live_transform")
            {
                RemotePresences[sig.UserId] = new CollabPresenceInfo
                {
                    RoomId = sig.RoomId,
                    UserId = sig.UserId,
                    Username = sig.Username ?? "",
                    ColorHex = string.IsNullOrEmpty(sig.ColorHex) ? CollabRoomService.GetColorForUser(sig.UserId) : sig.ColorHex,
                    SlideIndex = sig.SlideIndex,
                    SelectedId = !string.IsNullOrEmpty(sig.ShapeId) ? sig.ShapeId : (sig.SelectedId ?? ""),
                    CursorX = sig.CursorX,
                    CursorY = sig.CursorY,
                    IsEditing = sig.IsEditing,
                    LastSeen = DateTime.UtcNow
                };
            }

            if (sig.Type == "live_transform" && Mode == OfficeAppMode.Slides && slides?.Presentation != null &&
                !string.IsNullOrEmpty(sig.SlideId) && !string.IsNullOrEmpty(sig.ShapeId))
            {
                var slide = slides.Presentation.Slides.FirstOrDefault(s => s.Id == sig.SlideId);
                var shape = slide?.Shapes?.FirstOrDefault(sh => sh.Id == sig.ShapeId);
                if (shape != null)
                {
                    if (shape.Transform == null) shape.Transform = new Transform2D();
                    shape.Transform.X = sig.X;
                    shape.Transform.Y = sig.Y;
                    shape.Transform.Width = Math.Max(4f, sig.W);
                    shape.Transform.Height = Math.Max(4f, sig.H);
                    shape.Transform.Rotation = sig.Rotation;
                    slide.IsThumbnailDirty = true;
                }
            }

            RequestUiRefresh?.Invoke();
        }

        public void ApplyRemoteOps(List<CollabOpRecord> ops, SlidesView slides, CalcView calc, WriterView writer, RibbonBar ribbon)
        {
            if (ops == null || ops.Count == 0) return;

            var toApply = new List<CollabOpRecord>();
            foreach (var op in ops)
            {
                if (op.Seq > LastOpSeqApplied) LastOpSeqApplied = op.Seq;
                bool isSelf;
                lock (_sentOpLock)
                {
                    isSelf = _sentClientOpIds.Contains(op.ClientOpId);
                }
                if (!isSelf) toApply.Add(op);
            }
            if (toApply.Count == 0) return;

            _isApplyingRemoteOrUndo = true;
            try
            {
                ApplyOpsLocally(toApply, slides, calc, writer, ribbon);
                RequestUiRefresh?.Invoke();
            }
            finally
            {
                _isApplyingRemoteOrUndo = false;
            }
        }

        private void ApplyOpsLocally(List<CollabOpRecord> ops, SlidesView slides, CalcView calc, WriterView writer, RibbonBar ribbon)
        {
            bool slidesTouched = false;
            bool calcTouched = false;
            bool writerTouched = false;

            foreach (var op in ops)
            {
                try
                {
                    if (op.OpType == "slide_op" && slides?.Presentation != null)
                    {
                        var p = JsonSerializer.Deserialize<CollabSlideOpPayload>(op.PayloadJson);
                        if (p != null)
                        {
                            ApplySlidePayload(slides, p);
                            slidesTouched = true;
                        }
                    }
                    else if (op.OpType == "calc_patch" && calc != null)
                    {
                        var p = JsonSerializer.Deserialize<CollabCalcPatchPayload>(op.PayloadJson);
                        if (p != null && p.Cells != null)
                        {
                            foreach (var c in p.Cells)
                            {
                                while (calc.Sheets.Count <= c.SheetIndex)
                                {
                                    calc.Sheets.Add(new SparseMatrix());
                                    if (calc.SheetNames != null) calc.SheetNames.Add($"Sheet{calc.Sheets.Count}");
                                    if (calc.SheetStates != null) calc.SheetStates.Add(new CalcSheetState());
                                }
                                ApplyCellDtoToSheet(calc.Sheets[c.SheetIndex], c);
                            }
                            calcTouched = true;
                        }
                    }
                    else if (op.OpType == "writer_patch" && writer?.PieceTable != null)
                    {
                        var p = JsonSerializer.Deserialize<CollabWriterPatchPayload>(op.PayloadJson);
                        if (p != null)
                        {
                            string cur = writer.PieceTable.GetText() ?? "";
                            int off = ResolveAnchoredOffset(cur, p);
                            int delLen = Math.Min(cur.Length - off, p.DeleteLength);
                            string next = SpliceString(cur, off, delLen, p.InsertedText);
                            writer.PieceTable.SetInitialText(next);

                            int delta = (p.InsertedText?.Length ?? 0) - delLen;
                            // Shift local cursor so A keeps typing undisturbed!
                            if (delta != 0)
                            {
                                if (writer.CursorPosition >= off)
                                    writer.CursorPosition = Math.Max(0, Math.Min(next.Length, writer.CursorPosition + delta));
                                if (writer.SelectionStart >= off)
                                    writer.SelectionStart = Math.Max(0, Math.Min(next.Length, writer.SelectionStart + delta));
                                if (writer.SelectionEnd >= off)
                                    writer.SelectionEnd = Math.Max(0, Math.Min(next.Length, writer.SelectionEnd + delta));
                                ShiftPendingWriterUndoOffsets(off, delta);
                            }

                            if (p.Runs != null && p.Runs.Count > 0) ApplyWriterRuns(writer, p.Runs);
                            if (p.ParagraphAlignments != null)
                                writer.ParagraphAlignments = new Dictionary<int, int>(p.ParagraphAlignments);
                            writerTouched = true;
                        }
                    }
                }
                catch { }
            }

            if (slidesTouched && slides != null)
            {
                if (slides.Presentation != null && slides.ActiveSlideIndex >= slides.Presentation.Slides.Count)
                    slides.ActiveSlideIndex = Math.Max(0, slides.Presentation.Slides.Count - 1);
                slides.ClearEditorScrollCache();
                InitializeSlidesBaseline(slides);
            }
            if (calcTouched && calc != null)
            {
                calc.RefreshAfterRemoteEdit();
                InitializeCalcBaseline(calc);
            }
            if (writerTouched && writer != null)
            {
                if (ribbon != null) writer.UpdateRibbonState(ribbon);
                InitializeWriterBaseline(writer);
            }
        }

        private void ApplySlidePayload(SlidesView slides, CollabSlideOpPayload p)
        {
            var pres = slides.Presentation;
            EnsureSlideIds(pres);

            if (p.Action == "shape_upsert" && p.Shape != null)
            {
                var slide = pres.Slides.FirstOrDefault(s => s.Id == p.SlideId);
                if (slide == null) return;
                if (!string.IsNullOrEmpty(p.Shape.ImageAssetKey) &&
                    (string.IsNullOrEmpty(p.Shape.ImagePath) || !File.Exists(p.Shape.ImagePath)))
                {
                    string dl = Task.Run(() => CollabRoomService.EnsureAssetDownloadedAsync(RoomId, p.Shape.ImageAssetKey)).GetAwaiter().GetResult();
                    if (!string.IsNullOrEmpty(dl)) p.Shape.ImagePath = dl;
                }

                var existing = slide.Shapes.FirstOrDefault(sh => sh.Id == p.Shape.Id);
                if (existing != null)
                {
                    DtoToShape(p.Shape, existing);
                }
                else
                {
                    var created = DtoToShape(p.Shape, null);
                    int idx = (p.ShapeOrderIndex >= 0 && p.ShapeOrderIndex <= slide.Shapes.Count) ? p.ShapeOrderIndex : slide.Shapes.Count;
                    slide.Shapes.Insert(idx, created);
                }
                slide.IsThumbnailDirty = true;
            }
            else if (p.Action == "shape_delete")
            {
                var slide = pres.Slides.FirstOrDefault(s => s.Id == p.SlideId);
                if (slide == null) return;
                int rem = slide.Shapes.FindIndex(sh => sh.Id == p.ShapeId);
                if (rem >= 0)
                {
                    slide.Shapes.RemoveAt(rem);
                    if (slides.SelectedShapeIndex == rem) slides.SelectedShapeIndex = -1;
                    else if (slides.SelectedShapeIndex > rem) slides.SelectedShapeIndex--;
                    slide.IsThumbnailDirty = true;
                }
            }
            else if (p.Action == "slide_meta" && p.SlideMeta != null)
            {
                var slide = pres.Slides.FirstOrDefault(s => s.Id == p.SlideId);
                if (slide != null)
                {
                    ApplyMetaDto(slide, p.SlideMeta);
                    slide.IsThumbnailDirty = true;
                }
            }
            else if (p.Action == "slide_upsert" && p.SlideMeta != null)
            {
                var slide = pres.Slides.FirstOrDefault(s => s.Id == p.SlideId);
                if (slide == null)
                {
                    slide = new SlideNode { Id = p.SlideId };
                    int idx = Math.Max(0, Math.Min(pres.Slides.Count, p.SlideIndex));
                    pres.Slides.Insert(idx, slide);
                }
                ApplyMetaDto(slide, p.SlideMeta);
                if (p.SlideShapes != null)
                {
                    slide.Shapes.Clear();
                    foreach (var shDto in p.SlideShapes)
                    {
                        if (!string.IsNullOrEmpty(shDto.ImageAssetKey) && (string.IsNullOrEmpty(shDto.ImagePath) || !File.Exists(shDto.ImagePath)))
                        {
                            string dl = Task.Run(() => CollabRoomService.EnsureAssetDownloadedAsync(RoomId, shDto.ImageAssetKey)).GetAwaiter().GetResult();
                            if (!string.IsNullOrEmpty(dl)) shDto.ImagePath = dl;
                        }
                        slide.Shapes.Add(DtoToShape(shDto, null));
                    }
                }
                slide.IsThumbnailDirty = true;
            }
            else if (p.Action == "slide_delete")
            {
                if (pres.Slides.Count > 1)
                {
                    int idx = pres.Slides.FindIndex(s => s.Id == p.SlideId);
                    if (idx >= 0) pres.Slides.RemoveAt(idx);
                }
            }
            else if (p.Action == "slide_reorder" && p.SlideOrderIds != null && p.SlideOrderIds.Count > 0)
            {
                var map = pres.Slides.ToDictionary(s => s.Id, s => s);
                var reordered = new List<SlideNode>();
                foreach (var sid in p.SlideOrderIds)
                {
                    if (map.TryGetValue(sid, out var sn))
                    {
                        reordered.Add(sn);
                        map.Remove(sid);
                    }
                }
                reordered.AddRange(map.Values);
                pres.Slides.Clear();
                pres.Slides.AddRange(reordered);
            }
        }

        private CollabOpRecord MakeOp<T>(string opType, string targetKey, T payload)
        {
            var me = DatabaseAuthService.CurrentUser;
            return new CollabOpRecord
            {
                RoomId = RoomId,
                UserId = me?.Id ?? 0,
                Username = me?.Username ?? "",
                ClientOpId = Guid.NewGuid(),
                DocMode = (int)Mode,
                OpType = opType,
                TargetKey = targetKey ?? "",
                PayloadJson = JsonSerializer.Serialize(payload),
                CreatedAt = DateTime.UtcNow
            };
        }

        private void BroadcastOpsAsync(List<CollabOpRecord> ops)
        {
            if (ops == null || ops.Count == 0 || RoomId == Guid.Empty) return;
            lock (_sentOpLock)
            {
                foreach (var op in ops)
                {
                    if (op.ClientOpId == Guid.Empty) op.ClientOpId = Guid.NewGuid();
                    _sentClientOpIds.Add(op.ClientOpId);
                }
                if (_sentClientOpIds.Count > 800) _sentClientOpIds.Clear();
            }

            Task.Run(async () =>
            {
                foreach (var op in ops)
                {
                    long seq = await CollabRoomService.PushOpAsync(
                        RoomId, op.ClientOpId, op.DocMode, op.OpType, op.TargetKey, op.PayloadJson).ConfigureAwait(false);
                    if (seq > LastOpSeqApplied) LastOpSeqApplied = seq;
                }
            });
        }

        private static CollabOpRecord CloneWithFreshClientOpId(CollabOpRecord src)
        {
            return new CollabOpRecord
            {
                RoomId = src.RoomId,
                UserId = src.UserId,
                Username = src.Username,
                ClientOpId = Guid.NewGuid(),
                DocMode = src.DocMode,
                OpType = src.OpType,
                TargetKey = src.TargetKey,
                PayloadJson = src.PayloadJson,
                CreatedAt = DateTime.UtcNow
            };
        }

        #endregion

        #region Presence Heartbeat & 5s Auto-Save Snapshot

        public void NotifyPresenceIfChanged(SlidesView slides, CalcView calc, WriterView writer, bool force = false)
        {
            if (RoomId == Guid.Empty || !DatabaseAuthService.IsLoggedIn) return;
            int slideIdx = 0;
            string selId = "";
            float cx = 0f, cy = 0f;
            bool editing = false;

            if (Mode == OfficeAppMode.Slides && slides != null)
            {
                slideIdx = slides.ActiveSlideIndex;
                selId = slides.SelectedShape != null ? (slides.SelectedShape.Id ?? "") : "";
                if (slides.TryScreenToSlideLogical(slides.CurrentMouseCanvasPos, out float lx, out float ly))
                {
                    cx = lx;
                    cy = ly;
                }
                editing = slides.IsInlineEditing || slides.IsDraggingShape || slides.IsResizingShape;
            }
            else if (Mode == OfficeAppMode.Calc && calc != null)
            {
                slideIdx = calc.ActiveSheetIndex;
                selId = $"{calc.ActiveSheetIndex}:{calc.SelectedRow}:{calc.SelectedCol}";
                editing = calc.IsEditing;
            }
            else if (Mode == OfficeAppMode.Writer && writer != null)
            {
                slideIdx = 0;
                selId = writer.CursorPosition.ToString();
                cx = writer.CurrentCaretScreenX;
                cy = writer.CurrentCaretScreenY;
                editing = true;
            }

            long now = Environment.TickCount64;
            bool changed = (slideIdx != _lastSentSlideIdx || !string.Equals(selId, _lastSentSelectedId, StringComparison.Ordinal));
            if (!force && !changed && (now - _lastPresenceSentTick < 4000)) return;
            if (!force && changed && (now - _lastPresenceSentTick < 110)) return;

            _lastPresenceSentTick = now;
            _lastSentSlideIdx = slideIdx;
            _lastSentSelectedId = selId;

            _ = CollabRoomService.UpsertPresenceAsync(RoomId, slideIdx, selId, cx, cy, editing);
        }

        public async Task TryAutoSaveSnapshotAsync(Func<byte[]> exportDocBytes)
        {
            if (_disposed || !IsDirtyForSnapshot || IsSavingSnapshot || RoomId == Guid.Empty || exportDocBytes == null)
                return;

            IsSavingSnapshot = true;
            StatusLabel = AppL10n.T("Đang lưu Cloud...", "Saving...");
            RequestUiRefresh?.Invoke();

            try
            {
                byte[] bytes = exportDocBytes();
                if (bytes != null && bytes.Length > 0)
                {
                    var (ok, ver) = await CollabRoomService.SaveSnapshotAsync(RoomId, bytes, LastOpSeqApplied).ConfigureAwait(false);
                    if (ok)
                    {
                        IsDirtyForSnapshot = false;
                        LastSnapshotVersion = ver;
                        LastSavedUtc = DateTime.UtcNow;
                        StatusLabel = AppL10n.T("Đã lưu Cloud", "Saved to Cloud");
                    }
                    else
                    {
                        StatusLabel = AppL10n.T("Chờ lưu lại...", "Retry save...");
                    }
                }
            }
            catch
            {
                StatusLabel = AppL10n.T("Lỗi lưu tạm", "Save retry");
            }
            finally
            {
                IsSavingSnapshot = false;
                RequestUiRefresh?.Invoke();
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            if (RoomId != Guid.Empty)
            {
                _ = CollabRoomService.RemovePresenceAsync(RoomId);
                CollabRoomService.StopRoomSubscription(RoomId);
            }
        }

        #endregion
    }
}
