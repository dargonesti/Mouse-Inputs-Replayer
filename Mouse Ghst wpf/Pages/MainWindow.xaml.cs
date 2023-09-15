using Mouse_Ghst_wpf.Classes;
using Mouse_Ghst_wpf.Enums;
using Mouse_Ghst_wpf.Struct;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using MouseAction = Mouse_Ghst_wpf.Classes.MouseAction;
using Point = System.Windows.Point;

namespace Mouse_Ghst_wpf
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const int MOVE_TIMER = 1;
        private bool _isDragging = false;

        private ActionReplayer actionReplayer;

        private const int INPUT_MOUSE = 0;
        private const int INPUT_KEYBOARD = 1;
        private const int INPUT_BUTTON = 2;

        private bool shouldRecordMouse = false;

        private DateTime startTime;
        private IntPtr? keyboardHookHandle;
        private IntPtr? mouseHookHandle;

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


        private LowLevelKeyboardProc keyboardProc;
        private LowLevelMouseProc mouseProc;

        public MainWindow()
        {
            actionReplayer = new ActionReplayer();
            InitializeComponent();
            startTime = DateTime.Now;
            ReplayAmountTextBox.PreviewTextInput += PreviewTextInputHandler;
            ReplayAmount = 1;
        }

        private IntPtr SetKeyboardHook()
        {
            Console.WriteLine("Set KB Hook");
            keyboardProc = KeyboardHookCallback;
            using (var curProcess = Process.GetCurrentProcess())
            using (var curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(WH_KEYBOARD_LL, keyboardProc, IntPtr.Zero, 0); //GetModuleHandle(curModule.ModuleName)
            }
        }

        private IntPtr SetMouseHook()
        {
            Console.WriteLine("Set Mouse Hook");
            mouseProc = MouseHookCallback;
            return SetWindowsHookEx(WH_MOUSE_LL, mouseProc, IntPtr.Zero, 0);
        }

        private IntPtr KeyboardHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            int vkCode = Marshal.ReadInt32(lParam);

            if (RecordButton.IsEnabled == false  && nCode >= 0 && (wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_KEYUP))
            {
                InputAction action = new InputAction(ActionType.Keyboard, vkCode, (wParam == (IntPtr)WM_KEYDOWN), DateTime.Now - startTime);

                actionReplayer.allActions.Add(action);
            }

            if (vkCode == 27)
            {
                //Escape pressed, interrupt replay
                actionReplayer._interruptReplay = true;
            }
            Console.WriteLine($"Key : {vkCode}");
            return CallNextHookEx(keyboardHookHandle.Value, nCode, wParam, lParam);
        }

        private IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                HookStruct hookStruct = (HookStruct)Marshal.PtrToStructure(lParam, typeof(HookStruct));

                // Record mouse events
                MouseAction action = new MouseAction(wParam, hookStruct.pt.x, hookStruct.pt.y, DateTime.Now - startTime);

                if (StopButton.IsEnabled &&
                    (!(action.MouseType == MouseActionType.Move || action.MouseType == MouseActionType.Move2)
                    || shouldRecordMouse))
                {
                    actionReplayer.allActions.Add(action);
                    shouldRecordMouse = false;
                }
            }
            return CallNextHookEx(mouseHookHandle.Value, nCode, wParam, lParam);
        }

        private void RecordButton_Click(object sender, RoutedEventArgs e)
        {
            // Start recording
            actionReplayer.allActions.Clear();

            Hook(true, true);

            startTime = DateTime.Now;

            // Initialize the recordMouseTimer
            recordMouseTimer = new DispatcherTimer();
            recordMouseTimer.Interval = TimeSpan.FromMilliseconds(MOVE_TIMER); // Capture mouse position every X milliseconds
            recordMouseTimer.Tick += RecordMouseTimer_Tick;
            recordMouseTimer.Start();

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
            //PreviewKeyDown -= RecordKeyDown;
            Unhook();
            recordMouseTimer.Stop();
            recordMouseTimer.Tick -= RecordMouseTimer_Tick;
            shouldRecordMouse = false;

            // TODO : Make a recording editor that shows the inputs and edits them

            //UI
            RecordButton.IsEnabled = true;
            StopButton.IsEnabled = false;

            actionReplayer.LogActionsSummary();
            actionReplayer.RemoveStartStopClic();
            Console.WriteLine("After Cleanup");
            actionReplayer.LogActionsSummary();

            GC.Collect();

            Console.WriteLine($"Stopped to Record");
        }

        private async Task ReplayActionsAsync()
        {           
            actionReplayer._interruptReplay = false;

            GC.Collect();

            try
            {
                Hook(true, false);
                
                actionReplayer.isReplaying = true;
                actionReplayer._interruptReplay = true;
                await actionReplayer.ReplayActionsAsync(ReplayAmount);

                Unhook();
            }
            catch (Exception ex)
            {
                Console.WriteLine("General Replay Error");
                Console.WriteLine(ex.Message);
            }
            finally
            {
                Unhook();

                //UI
                actionReplayer.isReplaying = false;

                RecordButton.IsEnabled = true;
                StopButton.IsEnabled = false;
                ReplayButton.IsEnabled = true;
            }
        }

        private void ReplayButton_Click(object sender, RoutedEventArgs e)
        {
            Task.Run(ReplayActionsAsync);
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            base.OnClosing(e);
            Unhook(true);
        }

        private void Hook(bool keyboard = true, bool mouse = true)
        {
            if (!keyboardHookHandle.HasValue && keyboard)
            {
                Console.WriteLine("Hook Keyboard");
                keyboardHookHandle = SetKeyboardHook();
            }
            if (!mouseHookHandle.HasValue && mouse)
            {
                Console.WriteLine("Hook Mouse");
                mouseHookHandle = SetMouseHook();
            }
        }

        private void Unhook(bool keyboard = false, bool mouse = true)
        {
            if (keyboardHookHandle != null && keyboard)
            {
                Console.WriteLine("Unhook Keyboard");
                UnhookWindowsHookEx(keyboardHookHandle.Value);
                keyboardHookHandle = null;
            }
            if (mouseHookHandle != null && mouse)
            {
                Console.WriteLine("Unhook Mouse");
                UnhookWindowsHookEx(mouseHookHandle.Value);
                mouseHookHandle = null;
            }
        }

        private int _rep = 0;
        private int ReplayAmount
        {
            get
            {                
                return _rep;
            }
            set
            {
                _rep = (value <=0 ? Int32.MaxValue : value);
            }
        }

        private void Replayamount_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            try
            {
                ReplayAmount = Int32.Parse(ReplayAmountTextBox.Text);
            }
            catch { }
            //debounce too, to be sure
            Task.Run(() =>
            {
                Thread.Sleep(5);

                try
                {
                    ReplayAmount = Int32.Parse(ReplayAmountTextBox.Text);
                }
                catch { }
            });
        }

        private void PreviewTextInputHandler(Object sender, TextCompositionEventArgs e)
        {
            e.Handled = !IsTextAllowed(e.Text);
        }
        // Use the DataObject.Pasting Handler 
        private void TextBoxPasting(object sender, DataObjectPastingEventArgs e)
        {
            if (!e.DataObject.GetDataPresent(typeof(String)) 
                || !IsTextAllowed(e.DataObject.GetData(typeof(String)) as string))
            {
                e.CancelCommand();
            }
        }

        private static readonly Regex _regex = new Regex("[^0-9.-]+"); //regex that matches disallowed text
        private static bool IsTextAllowed(string text)
        {
            return !String.IsNullOrEmpty(text) && !_regex.IsMatch(text);
        }
    }

}
