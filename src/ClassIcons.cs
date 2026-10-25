using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace GitForce
{
    /// <summary>
    /// Toolbar icons drawn as vectors, in the spirit of the modern icon sets (Lucide, Fluent):
    /// a 24 x 24 grid, the same stroke width everywhere, round line caps and joins. Each icon is
    /// drawn with the text color of the theme and, for the part that tells what it does, one
    /// accent color. They are drawn at the size asked for, so they stay sharp at 16, 24 or 32 pixels.
    /// </summary>
    public static class ClassIcons
    {
        private enum Accent { None, Green, Red, Blue, Amber, Violet }

        /// <summary>
        /// Draws on the 24 x 24 grid with the stroke of the icon set
        /// </summary>
        private class Painter
        {
            private readonly Graphics g;
            private readonly Pen fore, accent;
            public Pen P;           // Pen in use: the text color, or the accent color

            public Painter(Graphics g, Pen fore, Pen accent)
            {
                this.g = g;
                this.fore = fore;
                this.accent = accent;
                P = fore;
            }

            public Painter Fore() { P = fore; return this; }
            public Painter Acc() { P = accent; return this; }

            public void Line(float x1, float y1, float x2, float y2) { g.DrawLine(P, x1, y1, x2, y2); }

            public void Poly(params float[] xy)
            {
                PointF[] pts = new PointF[xy.Length / 2];
                for (int i = 0; i < pts.Length; i++)
                    pts[i] = new PointF(xy[2 * i], xy[2 * i + 1]);
                g.DrawLines(P, pts);
            }

            public void Circle(float cx, float cy, float r) { g.DrawEllipse(P, cx - r, cy - r, 2 * r, 2 * r); }

            public void Ellipse(float cx, float cy, float rx, float ry) { g.DrawEllipse(P, cx - rx, cy - ry, 2 * rx, 2 * ry); }

            /// <summary>
            /// Arc of a circle; angles in degrees, clockwise from the right
            /// </summary>
            public void Arc(float cx, float cy, float r, float start, float sweep) { g.DrawArc(P, cx - r, cy - r, 2 * r, 2 * r, start, sweep); }

            public void HalfEllipse(float cx, float cy, float rx, float ry) { g.DrawArc(P, cx - rx, cy - ry, 2 * rx, 2 * ry, 0, 180); }

            public void Curve(float x1, float y1, float cx1, float cy1, float cx2, float cy2, float x2, float y2)
            {
                g.DrawBezier(P, x1, y1, cx1, cy1, cx2, cy2, x2, y2);
            }

            public void RoundRect(float x, float y, float w, float h, float r)
            {
                using (GraphicsPath path = new GraphicsPath())
                {
                    float d = 2 * r;
                    path.AddArc(x, y, d, d, 180, 90);
                    path.AddArc(x + w - d, y, d, d, 270, 90);
                    path.AddArc(x + w - d, y + h - d, d, d, 0, 90);
                    path.AddArc(x, y + h - d, d, d, 90, 90);
                    path.CloseFigure();
                    g.DrawPath(P, path);
                }
            }

            /// <summary>
            /// A page with a folded corner, from (x, y), w wide and h high
            /// </summary>
            public void File(float x, float y, float w, float h)
            {
                const float r = 2, fold = 6;
                using (GraphicsPath path = new GraphicsPath())
                {
                    path.AddLine(x + w - fold, y, x + r, y);
                    path.AddArc(x, y, 2 * r, 2 * r, 270, -90);
                    path.AddLine(x, y + r, x, y + h - r);
                    path.AddArc(x, y + h - 2 * r, 2 * r, 2 * r, 180, -90);
                    path.AddLine(x + r, y + h, x + w - r, y + h);
                    path.AddArc(x + w - 2 * r, y + h - 2 * r, 2 * r, 2 * r, 90, -90);
                    path.AddLine(x + w, y + h - r, x + w, y + fold);
                    path.CloseFigure();
                    g.DrawPath(P, path);
                }
                Poly(x + w - fold, y, x + w - fold, y + fold, x + w, y + fold);
            }

            /// <summary>
            /// The tray of the download and upload icons
            /// </summary>
            public void Tray()
            {
                Poly(3, 15, 3, 19);
                Arc(5, 19, 2, 180, -90);
                Line(5, 21, 19, 21);
                Arc(19, 19, 2, 90, -90);
                Poly(21, 19, 21, 15);
            }

            /// <summary>
            /// A circular arrow (undo, history): an arc and a head at its start
            /// </summary>
            public void CircleArrow(float cx, float cy, float r)
            {
                Arc(cx, cy, r, 205, 285);
                double a = 205 * Math.PI / 180;
                float px = cx + (float)(r * Math.Cos(a)), py = cy + (float)(r * Math.Sin(a));
                Poly(px - 0.5f, py - 5, px, py, px + 5, py + 0.5f);
            }

            /// <summary>
            /// A circular arrow turning clockwise (refresh, redo)
            /// </summary>
            public void CircleArrowCW(float cx, float cy, float r)
            {
                Arc(cx, cy, r, 335, -285);
                double a = 335 * Math.PI / 180;
                float px = cx + (float)(r * Math.Cos(a)), py = cy + (float)(r * Math.Sin(a));
                Poly(px + 0.5f, py - 5, px, py, px - 5, py + 0.5f);
            }

            public void Dot(float x, float y) { g.DrawLine(P, x, y, x + 0.01f, y); }

            /// <summary>
            /// The outline of an eye
            /// </summary>
            public void Eye()
            {
                Curve(2, 12, 5, 5.5f, 19, 5.5f, 22, 12);
                Curve(2, 12, 5, 18.5f, 19, 18.5f, 22, 12);
            }

            /// <summary>
            /// The box of the stash icons: a lid and a body
            /// </summary>
            public void Box()
            {
                RoundRect(2, 3, 20, 5, 1);
                Poly(4, 8, 4, 19);
                Arc(6, 19, 2, 180, -90);
                Line(6, 21, 18, 21);
                Arc(18, 19, 2, 90, -90);
                Poly(20, 19, 20, 8);
            }

            public void Funnel(float right)
            {
                float mid = right / 2 + 1, half = 2;
                using (GraphicsPath path = new GraphicsPath())
                {
                    path.AddLines(new[] {
                        new PointF(2, 4), new PointF(right, 4), new PointF(mid + half, 11.5f), new PointF(mid + half, 18.5f),
                        new PointF(mid - half, 20.5f), new PointF(mid - half, 11.5f) });
                    path.CloseFigure();
                    g.DrawPath(P, path);
                }
            }
        }

        private class Icon
        {
            public Accent Accent;
            public Action<Painter> Draw;
        }

        private static readonly Dictionary<string, Icon> Icons = new Dictionary<string, Icon>
        {
            // Add a new file to git: a page with a plus
            { "btAdd", new Icon { Accent = Accent.Green, Draw = p =>
            {
                p.File(4, 2, 16, 20);
                p.Acc().Line(12, 11, 12, 18);
                p.Line(8.5f, 14.5f, 15.5f, 14.5f);
            } } },
            // Update the changelist with the selected files: a page with a check
            { "btUpdate", new Icon { Accent = Accent.Blue, Draw = p =>
            {
                p.File(4, 2, 16, 20);
                p.Acc().Poly(8.5f, 15, 11, 17.5f, 15.5f, 12.5f);
            } } },
            // Update the changelist with all the files: two pages with a check
            { "btUpdateAll", new Icon { Accent = Accent.Blue, Draw = p =>
            {
                p.Poly(4, 17, 4, 5);
                p.Arc(6, 5, 2, 180, 90);
                p.Line(6, 3, 13, 3);
                p.File(8, 6, 13, 16);
                p.Acc().Poly(11.5f, 16, 13.5f, 18, 17.5f, 13.5f);
            } } },
            // Revert: an arrow turning back
            { "btRevert", new Icon { Accent = Accent.Amber, Draw = p =>
            {
                p.Acc().CircleArrow(12, 12, 8.5f);
            } } },
            // Open for delete: a page with a minus
            { "btDelete", new Icon { Accent = Accent.Red, Draw = p =>
            {
                p.File(4, 2, 16, 20);
                p.Acc().Line(8.5f, 14.5f, 15.5f, 14.5f);
            } } },
            // Delete from the file system: a trash can
            { "btDeleteFs", new Icon { Accent = Accent.Red, Draw = p =>
            {
                p.Line(3, 6, 21, 6);
                p.Poly(8, 6, 8, 4.5f, 9.5f, 3, 14.5f, 3, 16, 4.5f, 16, 6);
                p.Poly(5.5f, 6, 6.5f, 19.5f, 8.5f, 21, 15.5f, 21, 17.5f, 19.5f, 18.5f, 6);
                p.Acc().Line(10, 10.5f, 10, 16.5f);
                p.Line(14, 10.5f, 14, 16.5f);
            } } },
            // Edit: a pencil
            { "btEdit", new Icon { Accent = Accent.Amber, Draw = p =>
            {
                // Body from the top corners down to the tip, then a round end at the top
                p.Poly(20, 7.5f, 8, 19.5f, 3, 21, 4.5f, 16, 16.5f, 4);
                p.Arc(18.25f, 5.75f, 2.47f, 225, 180);
                p.Acc().Line(14.1f, 6.4f, 17.6f, 9.9f);
            } } },
            // Pull: an arrow coming down into the tray
            { "btPull", new Icon { Accent = Accent.Blue, Draw = p =>
            {
                p.Tray();
                p.Acc().Line(12, 3, 12, 15);
                p.Poly(7, 10, 12, 15, 17, 10);
            } } },
            // Push: an arrow going up from the tray
            { "btPush", new Icon { Accent = Accent.Green, Draw = p =>
            {
                p.Tray();
                p.Acc().Line(12, 15, 12, 3);
                p.Poly(7, 8, 12, 3, 17, 8);
            } } },
            // Pending changelists: a list with checks
            { "btChangelists", new Icon { Accent = Accent.Green, Draw = p =>
            {
                p.Acc().Poly(3, 6, 4.5f, 7.5f, 7.5f, 4.5f);
                p.Poly(3, 12, 4.5f, 13.5f, 7.5f, 10.5f);
                p.Fore().RoundRect(3, 16.5f, 4.5f, 4.5f, 1);
                p.Line(11, 6, 21, 6);
                p.Line(11, 12, 21, 12);
                p.Line(11, 18.75f, 21, 18.75f);
            } } },
            // Submitted changelists (history): a clock with an arrow turning back
            { "btSubmitted", new Icon { Accent = Accent.Violet, Draw = p =>
            {
                p.CircleArrow(12, 12, 9);
                p.Acc().Poly(12, 7, 12, 12, 15.5f, 14);
            } } },
            // Branches: two lines leaving each other
            { "btBranches", new Icon { Accent = Accent.Violet, Draw = p =>
            {
                p.Line(6, 3, 6, 15);
                p.Curve(18, 9, 18, 14, 14, 18, 9, 18);
                p.Acc().Circle(18, 6, 3);
                p.Circle(6, 18, 3);
            } } },
            // Repositories: a database
            { "btRepos", new Icon { Accent = Accent.Blue, Draw = p =>
            {
                p.Acc().Ellipse(12, 5, 9, 3);
                p.Fore().Line(3, 5, 3, 19);
                p.Line(21, 5, 21, 19);
                p.HalfEllipse(12, 12, 9, 3);
                p.HalfEllipse(12, 19, 9, 3);
            } } },
            // Cancel the operation: a circle with a cross
            { "btCancelOperation", new Icon { Accent = Accent.Red, Draw = p =>
            {
                p.Circle(12, 12, 10);
                p.Acc().Line(15, 9, 9, 15);
                p.Line(9, 9, 15, 15);
            } } },
            // Options: sliders
            { "btOptions", new Icon { Accent = Accent.Blue, Draw = p =>
            {
                p.Line(4, 21, 4, 14);
                p.Line(4, 10, 4, 3);
                p.Line(12, 21, 12, 12);
                p.Line(12, 8, 12, 3);
                p.Line(20, 21, 20, 16);
                p.Line(20, 12, 20, 3);
                p.Acc().Line(1.5f, 14, 6.5f, 14);
                p.Line(9.5f, 8, 14.5f, 8);
                p.Line(17.5f, 16, 22.5f, 16);
            } } },
            // SSH keys: a key
            { "btSsh", new Icon { Accent = Accent.Amber, Draw = p =>
            {
                p.Acc().Circle(7.5f, 15.5f, 5.5f);
                p.Fore().Line(11.4f, 11.6f, 21, 2);
                p.Poly(15.5f, 7.5f, 18.5f, 10.5f, 22, 7, 19, 4);
            } } },
            // A new version is available: an arrow up in a circle
            { "btNewVersion", new Icon { Accent = Accent.Green, Draw = p =>
            {
                p.Acc().Circle(12, 12, 10);
                p.Poly(8, 12, 12, 8, 16, 12);
                p.Line(12, 16, 12, 8);
            } } },
            // ---- Icons of the panel toolbars and of the menus ----
            { "refresh", new Icon { Accent = Accent.Blue, Draw = p => { p.Acc().CircleArrowCW(12, 12, 8.5f); } } },
            { "eye", new Icon { Accent = Accent.Blue, Draw = p => { p.Eye(); p.Acc().Circle(12, 12, 3); } } },
            { "eyeoff", new Icon { Accent = Accent.Red, Draw = p => { p.Eye(); p.Circle(12, 12, 3); p.Acc().Line(3.5f, 3.5f, 20.5f, 20.5f); } } },
            { "list", new Icon { Accent = Accent.Blue, Draw = p =>
            {
                p.Line(8, 6, 21, 6); p.Line(8, 12, 21, 12); p.Line(8, 18, 21, 18);
                p.Acc().Dot(3.5f, 6); p.Dot(3.5f, 12); p.Dot(3.5f, 18);
            } } },
            { "filter", new Icon { Accent = Accent.Blue, Draw = p => { p.Acc().Funnel(22); } } },
            { "filterx", new Icon { Accent = Accent.Red, Draw = p =>
            {
                p.Funnel(17);
                p.Acc().Line(15.5f, 14, 21.5f, 20); p.Line(21.5f, 14, 15.5f, 20);
            } } },
            { "close", new Icon { Accent = Accent.None, Draw = p => { p.Line(6, 6, 18, 18); p.Line(18, 6, 6, 18); } } },
            { "stage", new Icon { Accent = Accent.Green, Draw = p =>
            {
                p.RoundRect(3, 3, 18, 18, 3);
                p.Acc().Line(12, 8, 12, 16); p.Line(8, 12, 16, 12);
            } } },
            { "unstage", new Icon { Accent = Accent.Amber, Draw = p =>
            {
                p.RoundRect(3, 3, 18, 18, 3);
                p.Acc().Line(8, 12, 16, 12);
            } } },
            { "untrack", new Icon { Accent = Accent.Red, Draw = p =>
            {
                p.File(4, 2, 16, 20);
                p.Acc().Line(9.5f, 12.5f, 14.5f, 17.5f); p.Line(14.5f, 12.5f, 9.5f, 17.5f);
            } } },
            { "stash", new Icon { Accent = Accent.Violet, Draw = p => { p.Box(); p.Acc().Line(10, 12.5f, 14, 12.5f); } } },
            { "unstash", new Icon { Accent = Accent.Green, Draw = p =>
            {
                p.Box();
                p.Acc().Line(12, 18, 12, 11.5f); p.Poly(9.5f, 14, 12, 11.5f, 14.5f, 14);
            } } },
            { "diff", new Icon { Accent = Accent.Green, Draw = p =>
            {
                p.RoundRect(3, 3, 8, 18, 2); p.RoundRect(13, 3, 8, 18, 2);
                p.Line(5.5f, 8, 8.5f, 8); p.Line(5.5f, 12, 8.5f, 12);
                p.Acc().Line(15.5f, 8, 18.5f, 8); p.Line(15.5f, 12, 18.5f, 12); p.Line(15.5f, 16, 18.5f, 16);
            } } },
            { "user", new Icon { Accent = Accent.Blue, Draw = p =>
            {
                p.Acc().Circle(12, 8, 4);
                p.Fore().Curve(4, 21, 4, 16.5f, 8, 14, 12, 14); p.Curve(12, 14, 16, 14, 20, 16.5f, 20, 21);
            } } },
            { "folder", new Icon { Accent = Accent.Amber, Draw = p =>
            {
                p.Poly(4, 20, 20, 20, 22, 18, 22, 8.5f, 20, 6.5f, 12.5f, 6.5f, 10, 3.5f, 4, 3.5f, 2, 5.5f, 2, 18, 4, 20);
                p.Acc().Line(6, 11, 18, 11);
            } } },
            { "terminal", new Icon { Accent = Accent.Green, Draw = p =>
            {
                p.RoundRect(2, 4, 20, 16, 2);
                p.Acc().Poly(6, 9, 9, 12, 6, 15); p.Line(12, 15, 17, 15);
            } } },
            { "globe", new Icon { Accent = Accent.Blue, Draw = p =>
            {
                p.Circle(12, 12, 10); p.Line(2, 12, 22, 12);
                p.Acc().Ellipse(12, 12, 4.5f, 10);
            } } },
            { "merge", new Icon { Accent = Accent.Violet, Draw = p =>
            {
                p.Line(6, 9, 6, 21);
                p.Curve(6, 9, 6, 14, 10, 18, 15, 18);
                p.Acc().Circle(6, 6, 3); p.Circle(18, 18, 3);
            } } },
            { "switch", new Icon { Accent = Accent.Blue, Draw = p =>
            {
                p.Acc().Line(4, 7, 20, 7); p.Poly(16, 3, 20, 7, 16, 11);
                p.Fore().Line(20, 17, 4, 17); p.Poly(8, 13, 4, 17, 8, 21);
            } } },
            { "tag", new Icon { Accent = Accent.Amber, Draw = p =>
            {
                p.Poly(3, 3, 11.5f, 3, 21, 12.5f, 12.5f, 21, 3, 11.5f, 3, 3);
                p.Acc().Circle(7.5f, 7.5f, 1.5f);
            } } },
            { "message", new Icon { Accent = Accent.Blue, Draw = p =>
            {
                p.RoundRect(3, 4, 18, 13, 3);
                p.Poly(7, 17, 7, 21, 11.5f, 17);
                p.Acc().Line(7.5f, 9, 16.5f, 9); p.Line(7.5f, 12.5f, 13, 12.5f);
            } } },
            { "copy", new Icon { Accent = Accent.Blue, Draw = p =>
            {
                p.Acc().RoundRect(8, 8, 13, 13, 2);
                p.Fore().Line(2, 4, 2, 14); p.Arc(4, 14, 2, 180, -90); p.Arc(4, 4, 2, 180, 90);
                p.Line(4, 2, 14, 2); p.Arc(14, 4, 2, 270, 90);
            } } },
            { "info", new Icon { Accent = Accent.Blue, Draw = p =>
            {
                p.Circle(12, 12, 10);
                p.Acc().Line(12, 16, 12, 11.5f); p.Dot(12, 8);
            } } },
            { "commit", new Icon { Accent = Accent.Green, Draw = p =>
            {
                p.Line(2, 12, 8, 12); p.Line(16, 12, 22, 12);
                p.Acc().Circle(12, 12, 4);
            } } },
            { "save", new Icon { Accent = Accent.Blue, Draw = p =>
            {
                p.Poly(5, 3, 16, 3, 21, 8, 21, 19, 19, 21, 5, 21, 3, 19, 3, 5, 5, 3);
                p.Poly(7, 3, 7, 8, 15, 8);
                p.Acc().Poly(7, 21, 7, 14, 17, 14, 17, 21);
            } } },
            { "sun", new Icon { Accent = Accent.Amber, Draw = p =>
            {
                p.Acc().Circle(12, 12, 4);
                p.Fore();
                for (int i = 0; i < 8; i++)
                {
                    double a = i * Math.PI / 4;
                    p.Line(12 + (float)(7 * Math.Cos(a)), 12 + (float)(7 * Math.Sin(a)), 12 + (float)(9.5 * Math.Cos(a)), 12 + (float)(9.5 * Math.Sin(a)));
                }
            } } },
            { "book", new Icon { Accent = Accent.Blue, Draw = p =>
            {
                p.Poly(12, 7, 9, 4.5f, 2, 4.5f, 2, 18.5f, 9, 18.5f, 12, 21);
                p.Poly(12, 7, 15, 4.5f, 22, 4.5f, 22, 18.5f, 15, 18.5f, 12, 21);
                p.Acc().Line(12, 7, 12, 21);
            } } },
            { "home", new Icon { Accent = Accent.Blue, Draw = p =>
            {
                p.Poly(3, 10, 12, 3, 21, 10);
                p.Poly(5, 8.5f, 5, 21, 19, 21, 19, 8.5f);
                p.Acc().Poly(10, 21, 10, 15, 14, 15, 14, 21);
            } } },
            { "exit", new Icon { Accent = Accent.Red, Draw = p =>
            {
                p.Poly(9, 21, 5, 21, 3, 19, 3, 5, 5, 3, 9, 3);
                p.Acc().Line(21, 12, 9, 12); p.Poly(16, 7, 21, 12, 16, 17);
            } } },
            // HTTPS passwords: a lock
            { "btHttps", new Icon { Accent = Accent.Amber, Draw = p =>
            {
                p.RoundRect(3, 11, 18, 11, 2);
                p.Poly(7, 11, 7, 7);
                p.Arc(12, 7, 5, 180, 180);
                p.Poly(17, 7, 17, 11);
                p.Acc().Circle(12, 16.5f, 1.2f);
            } } },
        };

        /// <summary>
        /// Icons of the menu entries, found by the start of their text (the first match wins)
        /// </summary>
        private static readonly string[,] MenuIcons =
        {
            { "Stage", "stage" }, { "Unstage", "unstage" }, { "Untrack", "untrack" },
            { "Unignore", "eye" }, { "Ignore", "eyeoff" }, { "Show Ignored", "eyeoff" },
            { "Stash Selected", "stash" }, { "Stashes", "unstash" }, { "Stash", "stash" }, { "Unstash", "unstash" },
            { "Apply stash", "unstash" }, { "Pop stash", "unstash" }, { "Drop stash", "btDeleteFs" },
            { "Update Changelist with All", "btUpdateAll" }, { "Update Changelist", "btUpdate" }, { "Update All", "btUpdateAll" },
            { "Add", "btAdd" }, { "Revert", "btRevert" }, { "Reset", "btRevert" },
            { "Diff", "diff" }, { "Edit Commit Message", "message" }, { "Edit Commit Author", "user" },
            { "Edit Using", "btEdit" }, { "Edit Spec", "btEdit" }, { "Edit Merge Spec", "btEdit" }, { "Edit Remote", "globe" },
            { "Open in Editor", "btEdit" }, { "Associated Editor", "btEdit" }, { "Built-in Viewer", "eye" }, { "Open in Built-in Viewer", "eye" },
            { "Revision History", "btSubmitted" }, { "File History", "btSubmitted" }, { "Blame", "user" },
            { "Move/Rename", "btEdit" }, { "Rename", "btEdit" }, { "Open for Delete", "btDelete" },
            { "Remove from File System", "btDeleteFs" }, { "Delete", "btDeleteFs" }, { "Drop", "btDeleteFs" },
            { "Explore", "folder" }, { "Open Containing Folder", "folder" }, { "Command Prompt", "terminal" },
            { "Copy", "copy" }, { "Refresh", "refresh" }, { "Auto Refresh", "refresh" },
            { "Fetch", "btPull" }, { "Pull", "btPull" }, { "Push", "btPush" }, { "Switch Remote", "globe" },
            { "Options", "btOptions" }, { "Manage SSH", "btSsh" }, { "Manage HTTPS", "btHttps" },
            { "Pending Changelists", "btChangelists" }, { "View Pending", "btChangelists" },
            { "Submitted Changelists", "btSubmitted" }, { "View Submitted", "btSubmitted" },
            { "Commit Graph", "btBranches" }, { "View Branches", "btBranches" }, { "Branches", "btBranches" },
            { "View Repos", "btRepos" }, { "Repository", "btRepos" }, { "Git Command Log", "terminal" }, { "Log Window", "terminal" },
            { "Command Line", "terminal" }, { "Merge", "merge" }, { "Resolve", "merge" }, { "Run Merge Tool", "merge" },
            { "Abort Merge", "btCancelOperation" }, { "Switch to", "switch" }, { "New Branch", "btBranches" },
            { "New Tag", "tag" }, { "Tag ", "tag" }, { "Branch ", "btBranches" }, { "New...", "btBranches" },
            { "Submit", "commit" }, { "Describe Changelist", "info" }, { "Cherry Pick", "commit" },
            { "Save Patch", "save" }, { "Save Workspace", "save" }, { "Load Workspace", "folder" }, { "Import Workspace", "folder" },
            { "Theme", "sun" }, { "Toolbar Icons", "list" }, { "Getting Started", "book" }, { "Guide de d", "book" },
            { "Home", "home" }, { "Check for Updates", "btNewVersion" }, { "About", "info" }, { "Exit", "exit" },
        };

        private static readonly Dictionary<string, Bitmap> cache = new Dictionary<string, Bitmap>();

        /// <summary>
        /// An icon for the current theme, shared: the caller must not dispose it
        /// </summary>
        public static Bitmap Get(string name, int size)
        {
            string key = name + "|" + size + "|" + ClassTheme.IsDark;
            Bitmap bmp;
            if (!cache.TryGetValue(key, out bmp))
                cache[key] = bmp = Render(name, size);
            return bmp;
        }

        /// <summary>
        /// Name of the icon for a menu entry, or null
        /// </summary>
        public static string ForMenuText(string text)
        {
            if (string.IsNullOrEmpty(text))
                return null;
            string t = text.Replace("&", string.Empty).Trim();
            for (int i = 0; i < MenuIcons.GetLength(0); i++)
                if (t.StartsWith(MenuIcons[i, 0], StringComparison.OrdinalIgnoreCase))
                    return MenuIcons[i, 1];
            return null;
        }

        /// <summary>
        /// Give the menu entries without an image the icon that matches their text (and in their submenus)
        /// </summary>
        public static void DecorateMenu(ToolStripItemCollection items)
        {
            if (!Properties.Settings.Default.ModernIcons)
                return;
            foreach (ToolStripItem item in items)
            {
                ToolStripMenuItem m = item as ToolStripMenuItem;
                if (m == null)
                    continue;
                if (m.Image == null)
                {
                    string name = ForMenuText(m.Text);
                    if (name != null)
                        m.Image = Get(name, 16);
                }
                if (m.HasDropDownItems)
                    DecorateMenu(m.DropDownItems);
            }
        }

        /// <summary>
        /// Decorate a menu each time it opens (its entries are often built just before)
        /// </summary>
        public static void Attach(ToolStripDropDown menu)
        {
            if (menu != null)
                menu.Opening += (s, e) => DecorateMenu(menu.Items);
        }

        /// <summary>
        /// Give the buttons of a panel toolbar their icons: item name -> icon name
        /// </summary>
        public static void ApplyToolbar(ToolStrip toolStrip, params string[] map)
        {
            if (!Properties.Settings.Default.ModernIcons)
                return;
            for (int i = 0; i + 1 < map.Length; i += 2)
            {
                ToolStripItem[] found = toolStrip.Items.Find(map[i], false);
                foreach (ToolStripItem item in found)
                {
                    item.ImageTransparentColor = Color.Empty;
                    item.ImageScaling = ToolStripItemImageScaling.None;
                    item.Image = Get(map[i + 1], 16);
                }
            }
        }

        /// <summary>
        /// True if there is a drawn icon for a toolbar item (by its name)
        /// </summary>
        public static bool Has(string name)
        {
            return name != null && Icons.ContainsKey(name);
        }

        public static IEnumerable<string> Names { get { return Icons.Keys; } }

        private static Color AccentColor(Accent accent, bool dark)
        {
            switch (accent)
            {
                case Accent.Green: return dark ? Color.FromArgb(74, 222, 128) : Color.FromArgb(22, 150, 70);
                case Accent.Red: return dark ? Color.FromArgb(248, 113, 113) : Color.FromArgb(214, 40, 40);
                case Accent.Blue: return dark ? Color.FromArgb(96, 165, 250) : Color.FromArgb(37, 99, 235);
                case Accent.Amber: return dark ? Color.FromArgb(251, 191, 36) : Color.FromArgb(205, 120, 0);
                case Accent.Violet: return dark ? Color.FromArgb(180, 150, 255) : Color.FromArgb(124, 58, 237);
            }
            return Foreground(dark);
        }

        private static Color Foreground(bool dark)
        {
            return dark ? Color.FromArgb(214, 218, 224) : Color.FromArgb(51, 60, 74);
        }

        /// <summary>
        /// Draw an icon at a size in pixels, for the current theme (or the theme given)
        /// </summary>
        public static Bitmap Render(string name, int size, bool? dark = null)
        {
            Icon icon = Icons[name];
            bool isDark = dark ?? ClassTheme.IsDark;
            Bitmap bmp = new Bitmap(size, size);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.ScaleTransform(size / 24f, size / 24f);
                // At 16 pixels a stroke of 2 would be only 1.33 pixels wide: keep the small icons readable
                float width = size <= 16 ? 2.25f : 2f;
                using (Pen fore = MakePen(Foreground(isDark), width))
                using (Pen accent = MakePen(AccentColor(icon.Accent, isDark), width))
                    icon.Draw(new Painter(g, fore, accent));
            }
            return bmp;
        }

        private static Pen MakePen(Color color, float width)
        {
            return new Pen(color, width)
            {
                StartCap = LineCap.Round,
                EndCap = LineCap.Round,
                LineJoin = LineJoin.Round
            };
        }
    }
}
