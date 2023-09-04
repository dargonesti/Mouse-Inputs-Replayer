﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.ComponentModel;
using Mouse_Ghst_wpf.Classes;
using static System.Collections.Specialized.BitVector32;
using Mouse_Ghst_wpf.Enums;
using MouseAction = Mouse_Ghst_wpf.Classes.MouseAction;
using System.Windows.Threading;
using Point = System.Windows.Point;

namespace Mouse_Ghst_wpf
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const int INPUT_MOUSE = 0;
        private const int INPUT_KEYBOARD = 1;
        private const int INPUT_BUTTON = 2;

        private bool isReplaying = false;

        private List<BaseAction> allActions = new List<BaseAction>();
        private List<InputAction> recordedActions = new List<InputAction>();
        private List<MouseAction> recordedMouseActions = new List<MouseAction>();

        private DateTime startTime;
        private IntPtr keyboardHookHandle;
        private IntPtr mouseHookHandle;

        private DispatcherTimer recordMouseTimer;

        private const int WH_MOUSE_LL = 14;
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_KEYUP = 0x0101;
        private const int WM_LBUTTONDOWN = 0x0201;
        private const int WM_LBUTTONUP = 0x0202;
        private const int WM_MOUSEWHEEL = 0x020A;

        // Constants for mouse events
        private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        private const uint MOUSEEVENTF_LEFTUP = 0x0004;
        private const uint MOUSEEVENTF_WHEEL = 0x0800;

        private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);
        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);


        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);


        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        [DllImport("user32.dll")]
        private static extern void mouse_event(int dwFlags, int dx, int dy, int dwData, int dwExtraInfo);


        [DllImport("User32.dll")]
        private static extern bool SetCursorPos(int X, int Y);


        private LowLevelKeyboardProc keyboardProc;
        private LowLevelMouseProc mouseProc;

        public MainWindow()
        {
            InitializeComponent();
            startTime = DateTime.Now;
            keyboardHookHandle = SetHook();
            mouseHookHandle = SetMouseHook();
        }

        private IntPtr SetHook()
        {
            keyboardProc = KeyboardHookCallback;
            using (var curProcess = Process.GetCurrentProcess())
            using (var curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(WH_KEYBOARD_LL, keyboardProc, IntPtr.Zero, 0); //GetModuleHandle(curModule.ModuleName)
            }
        }

        private IntPtr SetMouseHook()
        {
            mouseProc = MouseHookCallback;
            return SetWindowsHookEx(WH_MOUSE_LL, mouseProc, IntPtr.Zero, 0);
        }

        private IntPtr KeyboardHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && (wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_KEYUP))
            {
                int vkCode = Marshal.ReadInt32(lParam);
                InputAction action = new InputAction(ActionType.Keyboard, vkCode, (wParam == (IntPtr)WM_KEYDOWN), DateTime.Now - startTime);
                recordedActions.Add(action);
                allActions.Add(action);
            }
            return CallNextHookEx(keyboardHookHandle, nCode, wParam, lParam);
        }

        private IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && (wParam == (IntPtr)WM_LBUTTONDOWN || wParam == (IntPtr)WM_LBUTTONUP || wParam == (IntPtr)WM_MOUSEWHEEL))
            {
                HookStruct hookStruct = (HookStruct)Marshal.PtrToStructure(lParam, typeof(HookStruct));

                // Record mouse events
                MouseAction action = new MouseAction(wParam, hookStruct.pt.x, hookStruct.pt.y, DateTime.Now - startTime);
                recordedMouseActions.Add(action);
                allActions.Add(action);
            }
            return CallNextHookEx(mouseHookHandle, nCode, wParam, lParam);
        }

        private void RecordButton_Click(object sender, RoutedEventArgs e)
        {
            // Start recording
            recordedActions.Clear();
            recordedMouseActions.Clear();
            allActions.Clear();

            startTime = DateTime.Now;

            // Initialize the recordMouseTimer
            recordMouseTimer = new DispatcherTimer();
            recordMouseTimer.Interval = TimeSpan.FromMilliseconds(10); // Capture mouse position every 10 milliseconds
            recordMouseTimer.Tick += RecordMouseTimer_Tick;
            recordMouseTimer.Start();

            // Subscribe to the PreviewKeyDown event to record keyboard input
            PreviewKeyDown += RecordKeyDown;

            //UI
            RecordButton.IsEnabled = false;
            StopButton.IsEnabled = true;

            Console.WriteLine($"Starting to Record");
        }

        private void RecordMouseTimer_Tick(object sender, EventArgs e)
        {
            // Record mouse events
            Point mousePosition = Mouse.GetPosition(this);
            var action = new MouseAction((IntPtr)MouseActionType.Move, (int)mousePosition.X, (int)mousePosition.Y, DateTime.Now - startTime);
            recordedMouseActions.Add(action);
            allActions.Add(action);
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            // Stop recording
            PreviewKeyDown -= RecordKeyDown;
            recordMouseTimer.Start();

            /*if (allActions.Count > 0)
            {
                allActions.RemoveAt(allActions.Count - 1);
                allActions.RemoveAt(allActions.Count - 1);
                recordedMouseActions.RemoveAt(recordedMouseActions.Count - 1);
                recordedMouseActions.RemoveAt(recordedMouseActions.Count - 1);
            }*/

            foreach (var action in allActions)
            {
                var keyAction = action as InputAction;
                var mouseAction = action as MouseAction;

                if (mouseAction != null)
                    Console.WriteLine($"Mouse Action: {mouseAction.MouseType}, X: {mouseAction.X}, Y: {mouseAction.Y}, Time: {mouseAction.Time}");
                else if (keyAction != null)
                    Console.WriteLine($"Key: {keyAction.KeyCode}, Time: {keyAction.Time}");
                else
                    Console.WriteLine("Unspecified Action Type.");
            }

            //UI
            RecordButton.IsEnabled = true;
            StopButton.IsEnabled = false;

            Console.WriteLine($"Stopped to Record");
        }
        private async Task ReplayActionsAsync(int replaySpeedMilliseconds)
        {
            var actionsCopy = allActions.ToArray();
            try
            {
                BaseAction lastAction = null;
                foreach (var action in actionsCopy)
                {
                    if (!isReplaying)
                    {
                        break; // Stop replaying if the user clicked the "Stop" button
                    }

                    var delay = lastAction is null ? TimeSpan.Zero : (action.Time - lastAction.Time);
                    lastAction = action;

                    await Task.Delay(delay);

                    if (action.Type == ActionType.Mouse)
                    {
                        // Replay mouse actions
                        var mouseAction = (MouseAction)action;
                        INPUT[] inputs = new INPUT[2];
                        inputs[0] = new INPUT();
                        inputs[0].type = INPUT_MOUSE;
                        inputs[0].u.mi.dx = (int)(mouseAction.X * 65536 / System.Windows.SystemParameters.PrimaryScreenWidth);
                        inputs[0].u.mi.dy = (int)(mouseAction.Y * 65536 / System.Windows.SystemParameters.PrimaryScreenHeight);
                        inputs[0].u.mi.dwFlags = (uint)mouseAction.MouseType;
                        //(mouseAction.MouseType == MouseActionType.MouseDown) ? MOUSEEVENTF_LEFTDOWN : MOUSEEVENTF_LEFTUP;

                        try
                        {
                            SetCursorPos(mouseAction.X, mouseAction.Y);

                            if (true || mouseAction.MouseType != MouseActionType.Move)
                            {
                                await Task.Delay(2);
                                mouse_event((int)mouseAction.MouseType, mouseAction.X, mouseAction.Y, 0, 0);
                                await Task.Delay(2);
                            }
                            Console.WriteLine("Mouse moved : " + mouseAction.MouseType.ToString());
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.ToString());
                        }
                    }
                    else if (action.Type == ActionType.Keyboard)
                    {
                        // Replay keyboard actions
                        var keyboardAction = (InputAction)action;
                        INPUT[] inputs = new INPUT[1];
                        inputs[0] = new INPUT();
                        inputs[0].type = INPUT_KEYBOARD;
                        inputs[0].u.ki.wVk = (ushort)keyboardAction.KeyCode;
                        inputs[0].u.ki.dwFlags = (uint)((keyboardAction.KeyDown) ? 0 : 2); // 0 for keydown, 2 for key

                        await Task.Delay(10);
                        SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(INPUT)));
                        await Task.Delay(10);
                        Console.WriteLine("Key Pressed");
                    }

                    //currentIndex++;
                }

                // Finished replaying all actions
                isReplaying = false;

                // Enable the "Replay" button when replaying is stopped
                ReplayButton.IsEnabled = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("General Replay Error");
                Console.WriteLine(ex.Message);
            }
        }
        private async Task ReplayActionsAsync2(int replaySpeedMilliseconds)
        {
            foreach (var action in allActions)
            {
                if (!isReplaying)
                {
                    break; // Stop replaying if the user clicked the "Stop" button
                }

                await Task.Delay(replaySpeedMilliseconds);

                if (action.Type == ActionType.Mouse)
                {
                    // Replay mouse actions
                    var mouseAction = (MouseAction)action;
                    if (mouseAction.MouseType == MouseActionType.Down)
                    {
                        //SendInput(MOUSEEVENTF_LEFTDOWN, (uint)mouseAction.X, (uint)mouseAction.Y, 0, 0);
                    }
                    else if (mouseAction.MouseType == MouseActionType.Up)
                    {
                        //SendInput(MOUSEEVENTF_LEFTUP, (uint)mouseAction.X, (uint)mouseAction.Y, 0, 0);
                    }
                    else if (mouseAction.MouseType == MouseActionType.MiddleDown || mouseAction.MouseType == MouseActionType.MiddleUp)
                    {
                        Console.WriteLine("TODO : Whele replay");
                        //SendInput(MOUSEEVENTF_WHEEL, 0, 0, (uint)mouseAction.WheelDelta, 0);
                    }
                }
                else if (action.Type == ActionType.Keyboard)
                {
                    // Replay keyboard actions
                    var keyboardAction = (InputAction)action;
                    if (keyboardAction.KeyDown)
                    {
                        KeyEventArgs keyEventArgs = new KeyEventArgs(Keyboard.PrimaryDevice, PresentationSource.FromVisual(this), 0, KeyInterop.KeyFromVirtualKey(keyboardAction.KeyCode));
                        keyEventArgs.RoutedEvent = UIElement.KeyDownEvent;
                        InputManager.Current.ProcessInput(keyEventArgs);
                    }
                    else
                    {
                        KeyEventArgs keyEventArgs = new KeyEventArgs(Keyboard.PrimaryDevice, PresentationSource.FromVisual(this), 0, KeyInterop.KeyFromVirtualKey(keyboardAction.KeyCode));
                        keyEventArgs.RoutedEvent = UIElement.KeyUpEvent;
                        InputManager.Current.ProcessInput(keyEventArgs);
                    }
                }

                //currentIndex++;
            }

            // Finished replaying all actions
            isReplaying = false;

            // Enable the "Replay" button when replaying is stopped
            ReplayButton.IsEnabled = true;
        }

        private void ReplayButton_Click(object sender, RoutedEventArgs e)
        {
            isReplaying = true;
            ReplayActionsAsync(333);
            // Replay recorded keyboard actions
            /* foreach (var action in recordedActions)
             {
                 // Simulate key press
                 if (action.KeyDown)
                 {
                     KeyEventArgs keyEventArgs = new KeyEventArgs(Keyboard.PrimaryDevice, PresentationSource.FromVisual(this), 0, KeyInterop.KeyFromVirtualKey(action.KeyCode));
                     keyEventArgs.RoutedEvent = UIElement.KeyDownEvent;
                     InputManager.Current.ProcessInput(keyEventArgs);
                 }
                 else
                 {
                     KeyEventArgs keyEventArgs = new KeyEventArgs(Keyboard.PrimaryDevice, PresentationSource.FromVisual(this), 0, KeyInterop.KeyFromVirtualKey(action.KeyCode));
                     keyEventArgs.RoutedEvent = UIElement.KeyUpEvent;
                     InputManager.Current.ProcessInput(keyEventArgs);
                 }
             }//*/
        }

        private void RecordKeyDown(object sender, KeyEventArgs e)
        {
            // Record key presses
            KeyboardAction action = new KeyboardAction(e.Key, DateTime.Now - startTime);
            //recordedActions.Add(action);
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            base.OnClosing(e);
            UnhookWindowsHookEx(keyboardHookHandle);
        }










        [StructLayout(LayoutKind.Sequential)]
        private struct INPUT
        {
            public int type;
            public InputUnion u;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct InputUnion
        {
            [FieldOffset(0)]
            public MOUSEINPUT mi;

            [FieldOffset(0)]
            public KEYBDINPUT ki;

            [FieldOffset(0)]
            public HARDWAREINPUT hi;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MOUSEINPUT
        {
            public int dx;
            public int dy;
            public uint mouseData;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct KEYBDINPUT
        {
            public ushort wVk;
            public ushort wScan;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct HARDWAREINPUT
        {
            public uint uMsg;
            public ushort wParamL;
            public ushort wParamH;
        }

        [Flags]
        public enum MouseEventFlags
        {
            Move = 0x00000001,
            LeftDown = 0x00000002,
            LeftUp = 0x00000004,
            MiddleDown = 0x00000020,
            MiddleUp = 0x00000040,
            Absolute = 0x00008000,
            RightDown = 0x00000008,
            RightUp = 0x00000010
        }

    }

}
