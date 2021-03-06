using System;
using System.Reflection;
using toxicFork.GUIHelpers.Disposable;
using toxicFork.GUIHelpers.DisposableGL;
using toxicFork.GUIHelpers.DisposableGUI;
using toxicFork.GUIHelpers.DisposableHandles;
using UnityEditor;
using UnityEngine;
using MaterialProperty = toxicFork.GUIHelpers.Disposable.MaterialProperty;
using Object = UnityEngine.Object;

namespace toxicFork.GUIHelpers {
    public class EditorHelpers {
        private static GUIStyle _fontWithBackgroundStyle;

        public static GUIStyle FontWithBackgroundStyle {
            get {
                return _fontWithBackgroundStyle ?? (_fontWithBackgroundStyle = new GUIStyle(GUI.skin.textArea));
            }
        }

        public static bool AllowMultiObjectAccess {
            get {
                var field = typeof (Editor).GetField("m_AllowMultiObjectAccess",
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                return field != null && (bool) field.GetValue(null);
            }
        }

        public static readonly float HandleSizeToPixels = (1.0f/64);

        public static void RecordUndo(String action, params Object[] objects) {
            Undo.RecordObjects(objects, action);
        }

        public static T PropertyToEnum<T>(SerializedProperty property) {
            return (T) Enum.Parse(typeof (T), property.enumNames[property.enumValueIndex]);
        }

        public static bool RadialSlider(int controlID, Vector3 position, float size, Material material) {
            return false;
        }

        public static bool CustomHandleButton(int controlID, Vector3 buttonPosition, float buttonSize, Texture2D texture,
            Texture2D hotTexture = null) {
            return CustomHandleButton(controlID, buttonPosition, buttonSize, texture, hotTexture, Color.white);
        }

        public static bool CustomHandleButton(int controlID, Vector3 buttonPosition, float buttonSize, Texture2D texture,
            Color color) {
            return CustomHandleButton(controlID, buttonPosition, buttonSize, texture, null, color);
        }

        public static bool CustomHandleButton(int controlID, Vector3 buttonPosition, float buttonSize, Texture2D texture,
            Texture2D hotTexture, Color color) {
            var distance = HandleUtility.DistanceToRectangle(buttonPosition, Quaternion.identity, buttonSize*0.5f);

            var buttonState = StateObject.Get<CustomButtonState>(controlID);

            switch (Event.current.type) {
                case EventType.layout:
                    HandleUtility.AddControl(controlID, distance);
                    break;
                case EventType.repaint:
                    using (var drawer = new GUITextureDrawer(texture, hotTexture)) {
                        drawer.alwaysVisible = true;
                        using (
                            new MaterialProperty(drawer.Material, "_Hot",
                                GUIUtility.hotControl == controlID && distance <= 0 ? 1f : 0f)
                            ) {
                            using (new MaterialColor(drawer.Material, color)) {
                                drawer.DrawSquare(buttonPosition, Quaternion.identity, buttonSize);
                            }
                        }
                        HandleUtility.Repaint();

                        if (buttonState.hovering) {
                            SetEditorCursor(MouseCursor.Link, controlID);
                        }
                    }
                    break;
                case EventType.mouseMove:
                    {
                        if (HandleUtility.nearestControl == controlID) {
                            buttonState.hovering = true;
                        }
                        else {
                            buttonState.hovering = false;
                        }
                        break;
                    }
            }

            switch (Event.current.GetTypeForControl(controlID)) {
                case EventType.mouseDown:
                    if (HandleUtility.nearestControl == controlID && Event.current.button == 0)
                    {
                        GUIUtility.hotControl = controlID;

                        Event.current.Use();
                        HandleUtility.Repaint();
                    }
                    break;
                case EventType.mouseUp:
                    if (GUIUtility.hotControl == controlID)
                    {
                        GUIUtility.hotControl = 0;
                        if (distance <= 0) {
                            GUI.changed = true;
                            Event.current.Use();
                            HandleUtility.Repaint();
                            return true;
                        }
                    }
                    break;
            }
            return false;
            //throw new NotImplementedException();
        }

        public class CustomButtonState {
            public bool hovering;
        }

        public static void SetEditorCursor(MouseCursor cursor) {
            EditorGUIUtility.AddCursorRect(new Rect(0, 0, Screen.width, Screen.height), cursor);
        }

        public static void SetEditorCursor(MouseCursor cursor, int controlID) {
            EditorGUIUtility.AddCursorRect(new Rect(0, 0, Screen.width, Screen.height), cursor, controlID);
        }

        public class AngleSliderInfo {
            public int button;
            public float mouseAngle;
            public Vector2 mousePosition;
            public float angle;
            public bool hovering;
        }

        public static float AngleSlider(int controlID, HandleDrawerBase drawer, Vector2 center, float angle,
            float distanceFromCenter, float handleSize, float snap = 0) {
            var info = StateObject.Get<AngleSliderInfo>(controlID);

            var current = Event.current;

            if (GUIUtility.hotControl == controlID) {
                angle = info.angle;
            }
            var handlePosition = center + Helpers2D.GetDirection(angle)*distanceFromCenter;

            if (Event.current.type == EventType.layout) {
                var distanceFromDrawer = drawer.GetDistance(handlePosition, handleSize, angle);
                HandleUtility.AddControl(controlID, distanceFromDrawer);
            }

            var typeForControl = current.GetTypeForControl(controlID);

            switch (typeForControl) {
                case EventType.mouseMove:
                    var hovering = HandleUtility.nearestControl == controlID &&
                                    (GUIUtility.hotControl == 0 || GUIUtility.hotControl == controlID);
                    if (info.hovering != hovering) {
                        current.Use();
                        info.hovering = hovering;
                    }
                    break;
            }

            if (GUIUtility.hotControl == controlID) {
                //active!
                switch (typeForControl) {
                    case EventType.mouseUp:
                        if (current.button == info.button) {
                            current.Use();
                            GUIUtility.hotControl = 0;
                        }
                        break;
                    case EventType.mouseDrag:
                        current.Use();

                        info.mousePosition += new Vector2(current.delta.x, current.delta.y);
                        Vector2 worldMousePosition = HandlePointToWorld(info.mousePosition);

                        var mouseAngle = Helpers2D.GetAngle(worldMousePosition - center);

                        info.angle += Mathf.DeltaAngle(info.mouseAngle, mouseAngle);
                        info.mouseAngle = mouseAngle;

                        angle = Handles.SnapValue(info.angle, snap);

                        GUI.changed = true;
                        break;
                }
            }
            else {
                if (GUIUtility.hotControl == 0) {
                    switch (typeForControl) {
                        case EventType.mouseDown:
                            if (HandleUtility.nearestControl == controlID && current.button == 0) {
                                info.button = current.button;
                                info.mousePosition = current.mousePosition;

                                Vector2 worldMousePosition = HandlePointToWorld(info.mousePosition);

                                var mouseAngle = Helpers2D.GetAngle(worldMousePosition - center);
                                info.mouseAngle = mouseAngle;
                                info.angle = angle;
                                current.Use();
                                GUIUtility.hotControl = controlID;
                            }
                            break;
                    }
                }
            }

            if (typeForControl == EventType.repaint) {
                if (GUIUtility.hotControl == controlID || (GUIUtility.hotControl == 0 && info.hovering)) {
                    SetEditorCursor(MouseCursor.RotateArrow, controlID);
                }

                drawer.Draw(controlID, handlePosition, handleSize, angle, info.hovering);
            }

            return angle;
        }


        public static Vector3 HandlePointToWorld(Vector2 mousePosition) {
            return HandleUtility.GUIPointToWorldRay(mousePosition).origin;
        }

        public static void DrawThickLine(Vector3 p1, Vector3 p2, float thickness, bool alwaysVisible = false) {
            var current = Camera.current;
            if (!current || Event.current.type != EventType.Repaint) {
                return;
            }
            var handleColor = Handles.color;
            if (alwaysVisible) {
                Helpers.AlwaysVisibleVertexGUIMaterial.SetPass(0);
            }
            else {
                Helpers.VertexGUIMaterial.SetPass(0);
            }

            using (new GLMatrix()) {
                GL.MultMatrix(Handles.matrix);

                var screenPoint1 = current.WorldToScreenPoint(p1);
                var screenPoint2 = current.WorldToScreenPoint(p2);

                var dir = (screenPoint2 - screenPoint1).normalized;
                Vector3 perpendicular = Helpers2D.GetPerpendicularVector(dir).normalized*thickness*0.5f;
                dir *= (thickness*0.5f);

                GL.Begin(GL.QUADS);
                GL.Color(new Color(handleColor.r, handleColor.g, handleColor.b, handleColor.a*0.5f));
                var extendedPerpendicular = (perpendicular + perpendicular.normalized*1f);
                var extendedDir = (dir + dir.normalized*1f);
                GL.Vertex(current.ScreenToWorldPoint(screenPoint1 - extendedDir - extendedPerpendicular));
                GL.Vertex(current.ScreenToWorldPoint(screenPoint1 - extendedDir + extendedPerpendicular));
                GL.Vertex(current.ScreenToWorldPoint(screenPoint2 + extendedDir + extendedPerpendicular));
                GL.Vertex(current.ScreenToWorldPoint(screenPoint2 + extendedDir - extendedPerpendicular));
                GL.End();

                GL.Begin(GL.QUADS);
                GL.Color(new Color(handleColor.r, handleColor.g, handleColor.b, handleColor.a*0.5f));
                extendedPerpendicular = (perpendicular + perpendicular.normalized*.5f);
                extendedDir = (dir + dir.normalized*.5f);
                GL.Vertex(current.ScreenToWorldPoint(screenPoint1 - extendedDir - extendedPerpendicular));
                GL.Vertex(current.ScreenToWorldPoint(screenPoint1 - extendedDir + extendedPerpendicular));
                GL.Vertex(current.ScreenToWorldPoint(screenPoint2 + extendedDir + extendedPerpendicular));
                GL.Vertex(current.ScreenToWorldPoint(screenPoint2 + extendedDir - extendedPerpendicular));
                GL.End();

                GL.Begin(GL.QUADS);
                GL.Color(handleColor);
                GL.Vertex(current.ScreenToWorldPoint(screenPoint1 - dir - perpendicular));
                GL.Vertex(current.ScreenToWorldPoint(screenPoint1 - dir + perpendicular));
                GL.Vertex(current.ScreenToWorldPoint(screenPoint2 + dir + perpendicular));
                GL.Vertex(current.ScreenToWorldPoint(screenPoint2 + dir - perpendicular));
                GL.End();
            }
        }


        public static void DrawThickLineWithOutline(Vector3 a, Vector3 b, float mainThickness, float outlineThickness,
            bool alwaysVisible = false) {
            var backgroundColor = Helpers.YIQ(Handles.color);
            backgroundColor.a = Handles.color.a;
            using (new HandleColor(backgroundColor)) {
                DrawThickLine(a, b, mainThickness + outlineThickness, alwaysVisible);
            }

            var cameraVectorA = HandleToScreenPoint(a);
            var cameraVectorB = HandleToScreenPoint(b);

            cameraVectorA.z -= 0.01f;
            cameraVectorB.z -= 0.01f;

            a = ScreenToHandlePoint(cameraVectorA);
            b = ScreenToHandlePoint(cameraVectorB);

            DrawThickLine(a, b, mainThickness, alwaysVisible);
        }

        public static float LineSlider(int controlID, Vector2 center, float distance, float angle,
            float handleScale = 1f, bool alwaysVisible = false, bool arrow = false) {
            var hoverState = StateObject.Get<HoverState>(controlID);
            var direction = Helpers2D.GetDirection(angle);
            var wantedPosition = center + direction*distance;

            var handleSize = HandleUtility.GetHandleSize(wantedPosition)*handleScale;

            var normal = Helpers2D.GetPerpendicularVector(direction)*
                             handleSize;

            EditorGUI.BeginChangeCheck();

            wantedPosition = Handles.Slider2D(controlID,
                wantedPosition,
                Vector3.forward,
                Vector3.up,
                Vector3.right,
                normal.magnitude*2,
                (id, position, rotation, size) => { },
                Vector2.zero);

            if (EditorGUI.EndChangeCheck()) {
                distance = Helpers2D.DistanceAlongLine(new Ray(center, direction), wantedPosition);
            }

            var current = Event.current;

            float drawScale = 1;
            switch (current.GetTypeForControl(controlID)) {
                case EventType.mouseMove:
                    var hovering = HandleUtility.nearestControl == controlID;
                    if (hoverState.hovering != hovering) {
                        current.Use();
                        hoverState.hovering = hovering;
                    }
                    break;
                case EventType.repaint:
                    var handleColor = Handles.color;

                    if (GUIUtility.hotControl == controlID || hoverState.hovering) {
                        if (GUIUtility.hotControl == controlID) {
                            handleColor = Color.red;
                        }
                        else {
                            handleColor = Color.yellow;
                        }

                        var cursor = RotatedResizeCursor(direction);

                        SetEditorCursor(cursor, controlID);
                        drawScale = 2;
                    }
                    else if (GUIUtility.hotControl != 0) {
                        handleColor.a *= 0.5f;
                    }

                    var drawNormal = normal*drawScale;
                    using (new HandleColor(handleColor)) {
                        Vector3 a = wantedPosition - drawNormal;
                        Vector3 b = wantedPosition + drawNormal;
                        if (GUIUtility.hotControl == controlID) {
                            var cameraVectorA = HandleToScreenPoint(a);
                            var cameraVectorB = HandleToScreenPoint(b);

                            cameraVectorA.z -= 0.01f;
                            cameraVectorB.z -= 0.01f;

                            a = ScreenToHandlePoint(cameraVectorA);
                            b = ScreenToHandlePoint(cameraVectorB);
                        }

                        if (arrow) {
                            Vector3 directionOffset = direction*handleSize*drawScale*0.5f;

                            var backgroundColor = Helpers.YIQ(Handles.color);
                            backgroundColor.a = Handles.color.a;
//
                            using (new HandleColor(backgroundColor)) {
                                DrawThickLine(a, b, 4, alwaysVisible);
                                DrawThickLine(a - directionOffset, a, 4, alwaysVisible);
                                DrawThickLine(b - directionOffset, b, 4, alwaysVisible);
                            }

                            DrawThickLine(a, b, 2, alwaysVisible);
                            DrawThickLine(a - directionOffset, a, 2, alwaysVisible);
                            DrawThickLine(b - directionOffset, b, 2, alwaysVisible);
                        }
                        else {
                            DrawThickLineWithOutline(a, b, 2, 2, alwaysVisible);
                        }
                    }
                    break;
            }
            return distance;
        }

        public static Vector3 ScreenToHandlePoint(Vector3 source) {
            return Handles.inverseMatrix.MultiplyPoint(Camera.current.ScreenToWorldPoint(source));
        }

        public static Vector3 HandleToScreenPoint(Vector3 source) {
            return Camera.current.WorldToScreenPoint(Handles.matrix.MultiplyPoint(source));
        }

        public static MouseCursor RotatedResizeCursor(Vector2 direction) {
            var cameraDirection = GetCameraDirection(direction);

            var angle = Helpers2D.GetAngle(cameraDirection);
            MouseCursor cursor;
            if (Mathf.Abs(Mathf.DeltaAngle(angle, 0)) <= 22.5f || Mathf.Abs(Mathf.DeltaAngle(angle, 180)) <= 22.5f) {
                cursor = MouseCursor.ResizeHorizontal;
            }
            else if (Mathf.Abs(Mathf.DeltaAngle(angle, 45)) <= 22.5f
                     || Mathf.Abs(Mathf.DeltaAngle(angle, 225)) <= 22.5f) {
                cursor = MouseCursor.ResizeUpRight;
            }
            else if (Mathf.Abs(Mathf.DeltaAngle(angle, 135)) <= 22.5f
                     || Mathf.Abs(Mathf.DeltaAngle(angle, 315)) <= 22.5f) {
                cursor = MouseCursor.ResizeUpLeft;
            }
            else {
                cursor = MouseCursor.ResizeVertical;
            }
            return cursor;
        }

        private static Vector3 GetCameraDirection(Vector2 direction) {
            var worldDirection = Handles.matrix.MultiplyVector(direction);
            var cameraDirection = Camera.current
                ? Camera.current.worldToCameraMatrix.MultiplyVector(worldDirection)
                : worldDirection;
            return cameraDirection;
        }

        public static void SelectObject(Object target, bool add = false) {
            if (add) {
                var objects = Selection.objects;
                if (!ArrayUtility.Contains(objects, target)) {
                    ArrayUtility.Add(ref objects, target);
                    Selection.objects = objects;
                }
            }
            else {
                Selection.activeObject = target;
            }
        }

        private static int _contextClickID;

        public static void ContextClick(int controlID, Action action) {
            var current = Event.current;
            switch (current.GetTypeForControl(controlID)) {
                case EventType.ContextClick:
                    if (HandleUtility.nearestControl == controlID && _contextClickID == controlID) {
                        _contextClickID = 0;
                        action();
                        GUIUtility.hotControl = 0;
                        GUIUtility.keyboardControl = 0;
                        current.Use();
                    }
                    break;
                case EventType.mouseDown:
                    if (HandleUtility.nearestControl == controlID) {
                        if (current.button == 1) {
                            _contextClickID = controlID;
                            GUIUtility.hotControl = 0;
                            GUIUtility.keyboardControl = 0;
                            Event.current.Use();
                        }
                    }
                    break;
            }
        }

        public static void ShowDropDown(Rect windowRect, Action<Action> onGUI) {
            EditorApplication.delayCall += () => {
                var helperPopupWindow = ScriptableObject.CreateInstance<HelperPopupWindow>();
                helperPopupWindow.ShowAsDropDown(onGUI, windowRect);
            };
        }

        public static void ShowDropDown(Rect windowRect, Action<Action, bool> onGUIFocus) {
            EditorApplication.delayCall += () => {
                var helperPopupWindow = ScriptableObject.CreateInstance<HelperPopupWindow>();
                helperPopupWindow.ShowAsDropDown(onGUIFocus, windowRect);
            };
        }

        public static EditorWindow ShowUtility(String title, Rect windowRect, Action<Action, bool> onGUIFocus) {
            var helperPopupWindow = ScriptableObject.CreateInstance<HelperPopupWindow>();

            EditorApplication.delayCall += () => {
                helperPopupWindow.position = windowRect;
                helperPopupWindow.ShowUtility(title, onGUIFocus);
                helperPopupWindow.Focus();
            };

            return helperPopupWindow;
        }

        public static EditorWindow ShowUtility(String title, Rect windowRect, Action<Action> onGUI)
        {
            var helperPopupWindow = ScriptableObject.CreateInstance<HelperPopupWindow>();

            EditorApplication.delayCall += () => {
                helperPopupWindow.position = windowRect;
                helperPopupWindow.ShowUtility(title, onGUI);
            };

            return helperPopupWindow;
        }

        public static EditorWindow ShowUtility(String title, Rect windowRect, Action<Action> onGUI, Action onShow)
        {
            var helperPopupWindow = ScriptableObject.CreateInstance<HelperPopupWindow>();
            
            EditorApplication.delayCall += () =>
            {
                helperPopupWindow.position = windowRect;
                helperPopupWindow.ShowUtility(title, onGUI);
                onShow();
            };

            return helperPopupWindow;
        }

        public static EditorWindow ShowUtility(String title, Rect windowRect, Action<Action, bool> onGUIFocus, Action onShow)
        {
            var helperPopupWindow = ScriptableObject.CreateInstance<HelperPopupWindow>();

            EditorApplication.delayCall += () =>
            {
                helperPopupWindow.position = windowRect;
                helperPopupWindow.ShowUtility(title, onGUIFocus);
                onShow();
            };

            return helperPopupWindow;
        }

        public static bool IsWarm(int controlID) {
            return GUIUtility.hotControl == controlID || (GUIUtility.hotControl == 0 && HandleUtility.nearestControl == controlID);
        }

        public static void OverlayLabel(Vector3 position, string text)
        {
            OverlayLabel(position, new GUIContent(text), GUI.skin.label);
        }

        public static void OverlayLabel(Vector3 position, Texture image)
        {
            OverlayLabel(position, new GUIContent(image), GUI.skin.label);
        }

        public static void OverlayLabel(Vector3 position, string text, GUIStyle style)
        {
            OverlayLabel(position, new GUIContent(text), style);
        }

        public static void OverlayLabel(Vector3 position, GUIContent content) {
            OverlayLabel(position, content, GUI.skin.label);
        }

        public static void OverlayLabel(Vector3 position, GUIContent content, GUIStyle style) {
            SceneView.OnSceneFunc del = null;

            del = delegate {
// ReSharper disable once DelegateSubtraction
                SceneView.onSceneGUIDelegate -= del;
                Handles.Label(position, content, style);
            };

            SceneView.onSceneGUIDelegate += del;
        }
    }
}
