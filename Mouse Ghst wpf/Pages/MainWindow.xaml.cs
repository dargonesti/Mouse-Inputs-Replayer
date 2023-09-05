using System;
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
using System.Drawing;
using Mouse_Ghst_wpf.Struct;

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
        private bool shouldRecordMouse = false;

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
        private static extern uint SendInput(uint nInputs, Input[] pInputs, int cbSize);


        [DllImport("user32.dll")]
        private static extern void mouse_event(int dwFlags, int dx, int dy, int dwData, int dwExtraInfo);


        [DllImport("User32.dll")]
        private static extern bool SetCursorPos(int X, int Y);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetCursorPos(out Point lpPoint);


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
               
                allActions.Add(action);
            }
            return CallNextHookEx(keyboardHookHandle, nCode, wParam, lParam);
        }

        private IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                HookStruct hookStruct = (HookStruct)Marshal.PtrToStructure(lParam, typeof(HookStruct));

                // Record mouse events
                MouseAction action = new MouseAction(wParam, hookStruct.pt.x, hookStruct.pt.y, DateTime.Now - startTime);

                if(StopButton.IsEnabled && 
                    (!(action.MouseType == MouseActionType.Move || action.MouseType == MouseActionType.Move2)
                    || shouldRecordMouse))
                {
                    allActions.Add(action);
                    shouldRecordMouse = false;
                }
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
            recordMouseTimer.Interval = TimeSpan.FromMilliseconds(10); // Capture mouse position every X milliseconds
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
            shouldRecordMouse = true;
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            // Stop recording
            PreviewKeyDown -= RecordKeyDown;
            recordMouseTimer.Stop();
            recordMouseTimer.Tick -= RecordMouseTimer_Tick;
            shouldRecordMouse = false;

            // TODO : Erase last click that wasn't to stop the recording

            foreach (var action in allActions)
            {
                var keyAction = action as InputAction;
                var mouseAction = action as MouseAction;

                if (mouseAction != null)
                {
                    string mouseActionString = (int)mouseAction.UnknownMouseType == 0 ? mouseAction.MouseType.ToString() : mouseAction.UnknownMouseType.ToString();
                    Console.WriteLine($"Mouse Action: {mouseActionString}, X: {mouseAction.X}, Y: {mouseAction.Y}, Time: {mouseAction.Time}");
                }
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
                        Input[] inputs = new Input[1];
                        inputs[0] = new Input();
                        inputs[0].type = INPUT_MOUSE;
                        inputs[0].u.mi.dx = (int)(mouseAction.X * 65536 / SystemParameters.PrimaryScreenWidth);
                        inputs[0].u.mi.dy = (int)(mouseAction.Y * 65536 / SystemParameters.PrimaryScreenHeight);
                        inputs[0].u.mi.dwFlags = (uint)mouseAction.MouseType;
                        inputs[0].u.mi.mouseData = 0;
                        //(mouseAction.MouseType == MouseActionType.MouseDown) ? MOUSEEVENTF_LEFTDOWN : MOUSEEVENTF_LEFTUP;

                        try
                        {
                            SetCursorPos(mouseAction.X, mouseAction.Y);

                            if (true || mouseAction.MouseType != MouseActionType.Move)
                            {
                                // await Task.Delay(1);
                                //mouse_event((int)mouseAction.MouseType, mouseAction.X, mouseAction.Y, 0, 0);
                                SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(Input)));
                                //await Task.Delay(1);
                            }
                            Console.WriteLine($"Mouse moved : {mouseAction.MouseType.ToString()}, ({mouseAction.X}, {mouseAction.Y}), delay: {delay.TotalMilliseconds/1000}");
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
                        inputs[0].u.ki.dwFlags = (uint)((keyboardAction.KeyDown) ? 0 : 2); // 0 for keydown, 2 for key

                        //await Task.Delay(10);
                        SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(Input)));
                        //await Task.Delay(10);
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
                        keyEventArgs.RoutedEvent = KeyDownEvent;
                        InputManager.Current.ProcessInput(keyEventArgs);
                    }
                    else
                    {
                        KeyEventArgs keyEventArgs = new KeyEventArgs(Keyboard.PrimaryDevice, PresentationSource.FromVisual(this), 0, KeyInterop.KeyFromVirtualKey(keyboardAction.KeyCode));
                        keyEventArgs.RoutedEvent = KeyUpEvent;
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

    }

}
