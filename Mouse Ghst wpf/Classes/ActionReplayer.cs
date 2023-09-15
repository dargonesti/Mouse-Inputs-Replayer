using Mouse_Ghst_wpf.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls.Primitives;
using System.Windows;
using Mouse_Ghst_wpf.Struct;
using Point = System.Windows.Point;

namespace Mouse_Ghst_wpf.Classes
{
    public class ActionReplayer
    {
        public bool _interruptReplay = false;
        public bool isReplaying = false;
        public bool _isDragging = false;
        private uint mouseEventFlag = 0;
        public List<BaseAction> allActions { get; set; } = new List<BaseAction>();

        private const uint MOUSEEVENTF_ABSOLUTE = 0x8000;
        private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        private const uint MOUSEEVENTF_LEFTUP = 0x0004;
        private const uint MOUSEEVENTF_MOVE = 0x0001;
        private const int INPUT_MOUSE = 0;
        private const int INPUT_KEYBOARD = 1;
        private const int INPUT_BUTTON = 2;

        public async Task ReplayActionsAsync(int loops)
        {
            _interruptReplay = false;

            var actionsCopy = allActions.ToArray();
            var firstAction = actionsCopy.First();

            GC.Collect();

            Console.WriteLine($"Replaying actions, {loops} times.");

            for (int i = 0; i < loops; i++)
            {
                Console.WriteLine($"Replaying actions, loop #{i}.");
                var replayStart = DateTime.Now;

                if (!isReplaying || _interruptReplay)
                {
                    break; // Stop replaying if the user clicked the "Stop" or "Esc" button
                }

                foreach (var action in actionsCopy)
                {
                    if (!isReplaying || _interruptReplay)
                    {
                        break; // Stop replaying if the user clicked the "Stop" or "Esc" button
                    }

                    var actualDelay = (action.Time - firstAction.Time) - (DateTime.Now - replayStart);
                    if (actualDelay > TimeSpan.Zero)
                    {
                        Thread.Sleep(actualDelay);
                    }

                    if (action.Type == ActionType.Mouse)
                    {
                        // Replay mouse actions
                        var mouseAction = (MouseAction)action;
                        if (mouseAction.MouseType == MouseActionType.Down || mouseAction.MouseType == MouseActionType.Down2)
                        {
                            _isDragging = true;
                        }
                        else if (mouseAction.MouseType == MouseActionType.Up || mouseAction.MouseType == MouseActionType.Up2)
                        {
                            _isDragging = false;
                        }

                        switch (mouseAction.MouseType)
                        {
                            case MouseActionType.Move:
                                mouseEventFlag = MOUSEEVENTF_MOVE;
                                break;
                            case MouseActionType.Down:
                            case MouseActionType.Down2:
                                mouseEventFlag = MOUSEEVENTF_LEFTDOWN;
                                break;
                            case MouseActionType.Up:
                            case MouseActionType.Up2:
                                mouseEventFlag = MOUSEEVENTF_LEFTUP;
                                break;
                                // Add other cases as necessary
                        }
                        try
                        {
                            if (true || mouseAction.MouseType != MouseActionType.Move)
                            {
                                // Use mouse_event function to simulate the action
                                mouse_event(mouseEventFlag | MOUSEEVENTF_MOVE | MOUSEEVENTF_ABSOLUTE | (_isDragging ? MOUSEEVENTF_LEFTDOWN : 0),
                                            (uint)(mouseAction.X * 65536 / SystemParameters.PrimaryScreenWidth),
                                            (uint)(mouseAction.Y * 65536 / SystemParameters.PrimaryScreenHeight),
                                            0, 0);
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Mouse Move exception : " + ex.ToString());
                        }
                    }
                    else if (action.Type == ActionType.Keyboard)
                    {
                        // Replay keyboard actions
                        var keyboardAction = (InputAction)action;
                        Input[] inputs = new Input[1];
                        inputs[0] = new Input();
                        inputs[0].type = INPUT_KEYBOARD;
                        inputs[0].u.ki.wVk = (ushort)keyboardAction.KeyCode;
                        inputs[0].u.ki.dwFlags = (uint)((keyboardAction.KeyDown) ? 0 : 2);

                        SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(Input)));
                        Console.WriteLine("Key Pressed");
                    }
                }
            }

            // Finished replaying all actions
            isReplaying = false;
        }

        public void RemoveStartStopClic(bool start=true, bool stop=true) // TODO : Performance mode
        {
            InputAction lastNonMoveAction;
            var lastActionInd = 0;
            var firstActionInd = 0;
            bool passedStopClick = false;

            var actionArray = allActions.ToArray();

            //remove ending moves
            for (var i = allActions.Count - 1; i >= 0; i--)
            {
                if (isUp(actionArray[i] as MouseAction)){
                    if(!passedStopClick)
                    {
                        passedStopClick = true;
                    }
                    else
                    {
                        if (i > 1)
                        {
                            lastActionInd = i+1;
                            break;
                        }
                    }
                }
            }

            if (lastActionInd > 1)
            {
                allActions.RemoveRange(lastActionInd, allActions.Count - lastActionInd);
            }

            //remove start moves
            for (var i = 0; i < allActions.Count; i++)
            {
                if (isUp(actionArray[i] as MouseAction)
                    || isDown(actionArray[i] as MouseAction)
                    || (actionArray[i] as MouseAction) == null)
                {
                    firstActionInd = i - 5; // Let the mouse move a bit before we click
                    break;
                }
            }

            if (firstActionInd > 0 && firstActionInd + 10 < allActions.Count){ // 10 actions minimum
                allActions.RemoveRange(0, firstActionInd);
            }
        }

        public void LogActionsSummary()
        {
            string summary = "\nvvvvvvvvvvvvvvvvvv\n";

            var down = false;
            var moveCount = 0;

            foreach(var action in allActions)
            {
                var mouseAction = action as MouseAction;
                if (mouseAction != null) { 
                    if(isDown(mouseAction))
                    {
                        if (moveCount > 0)
                        {
                            summary += $"moved x {moveCount}\n";
                            moveCount = 0;
                        }
                        summary += $"down ({mouseAction.MouseType.ToString()})\n";
                    }
                    else if (isUp(mouseAction))
                    {
                        if (moveCount > 0)
                        {
                            summary += $"moved x {moveCount}\n";
                            moveCount = 0;
                        }
                        summary += $"up ({mouseAction.MouseType.ToString()})\n";
                    }
                    else
                    {
                        moveCount++;
                    }
                }

            }

            if (moveCount > 0)
            {
                summary += $"moved {moveCount} times\n";
            }

            summary += "-------------------\n";

            Console.WriteLine(summary);
        }

        #region privates
        private bool isDown(MouseAction mouseAction)
        {
            return mouseAction?.MouseType == MouseActionType.Down || mouseAction?.MouseType == MouseActionType.Down2;
        }
        private bool isUp(MouseAction mouseAction)
        {
            return mouseAction?.MouseType == MouseActionType.Up || mouseAction?.MouseType == MouseActionType.Up2;
        }
        #endregion

        #region WindowsDLL

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint SendInput(uint nInputs, Input[] pInputs, int cbSize);


        [DllImport("user32.dll")]
        private static extern void mouse_event(int dwFlags, int dx, int dy, int dwData, int dwExtraInfo);

        [DllImport("user32.dll")]
        public static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint cButtons, uint dwExtraInfo);


        [DllImport("User32.dll")]
        private static extern bool SetCursorPos(int X, int Y);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetCursorPos(out Point lpPoint);

        #endregion
    }

}
