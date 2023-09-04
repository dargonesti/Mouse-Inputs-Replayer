using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mouse_Ghst_wpf.Placeholder
{
    internal class form1
    {
    }
}
/*
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
using System;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.ComponentModel;
using Mouse_Ghst_wpf.Classes;

namespace Mouse_Ghst_wpf
{
    public partial class MainWindow2 : Window
    {
        private List<KeyboardAction> recordedActions = new List<KeyboardAction>();
        private DateTime startTime;


        /*
         public MainWindow2()
         {
             InitializeComponent();
             mouseProc = MouseHookCallback;
             hookHandle = SetHook(mouseProc);
         }

         #region mouse
         private void Button_Click(object sender, RoutedEventArgs e)
         {

         }

         private void Button_Click_1(object sender, RoutedEventArgs e)
         {

         }

         private void Button_Click_2(object sender, RoutedEventArgs e)
         {

         }
         private const int WH_MOUSE_LL = 14;
         private const int WM_LBUTTONDOWN = 0x0201;
         private IntPtr hookHandle;

         private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

         [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
         private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

         [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
         [return: MarshalAs(UnmanagedType.Bool)]
         private static extern bool UnhookWindowsHookEx(IntPtr hhk);

         [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
         private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

         private LowLevelMouseProc mouseProc;


         private IntPtr SetHook(LowLevelMouseProc proc)
         {
             using (ProcessModule module = Process.GetCurrentProcess().MainModule)
             {
                 return SetWindowsHookEx(WH_MOUSE_LL, proc, GetModuleHandle(module.ModuleName), 0);
             }
         }

         private IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
         {
             if (nCode >= 0 && wParam == (IntPtr)WM_LBUTTONDOWN)
             {
                 // Handle the mouse click event here (e.g., record the click coordinates)
             }
             return CallNextHookEx(hookHandle, nCode, wParam, lParam);
         }

         protected override void OnClosing(CancelEventArgs e)
         {
             UnhookWindowsHookEx(hookHandle);
             base.OnClosing(e);
         }
         #endregion

         #region keyboard
         private List<InputAction> recordedActions = new List<InputAction>();
         private DateTime startTime;

         private const int WH_KEYBOARD_LL = 13;
         private const int WM_KEYDOWN = 0x0100;
         private IntPtr keyboardHookHandle;

         private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

         [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
         private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

         [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
         [return: MarshalAs(UnmanagedType.Bool)]
         private static extern bool UnhookWindowsHookEx(IntPtr hhk);

         [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
         private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

         private LowLevelKeyboardProc keyboardProc;

         public MainForm()
         {
             InitializeComponent();
             keyboardProc = KeyboardHookCallback;
             keyboardHookHandle = SetHook(keyboardProc);
             startTime = DateTime.Now;
         }

         private IntPtr SetHook(LowLevelKeyboardProc proc)
         {
             using (var curProcess = System.Diagnostics.Process.GetCurrentProcess())
             using (var curModule = curProcess.MainModule)
             {
                 return SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
             }
         }

         private IntPtr KeyboardHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
         {
             if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
             {
                 int vkCode = Marshal.ReadInt32(lParam);
                 InputAction action = new InputAction(ActionType.Keyboard, vkCode, DateTime.Now - startTime);
                 recordedActions.Add(action);
             }
             return CallNextHookEx(keyboardHookHandle, nCode, wParam, lParam);
         }

         private void RecordButton_Click(object sender, EventArgs e)
         {
             // Start recording
             recordedActions.Clear();
             startTime = DateTime.Now;
         }

         private void StopButton_Click(object sender, EventArgs e)
         {
             // Stop recording
             foreach (var action in recordedActions)
             {
                 Console.WriteLine($"Action Type: {action.Type}, Key Code: {action.KeyCode}, Time: {action.Time}");
             }
         }

         private void ReplayButton_Click(object sender, EventArgs e)
         {
             // Replay recorded actions
             foreach (var action in recordedActions)
             {
                 if (action.Type == ActionType.Keyboard)
                 {
                     // Simulate key press
                     keybd_event((byte)action.KeyCode, 0, 0, 0);
                 }
             }
         }

         [DllImport("user32.dll")]
         private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, uint dwExtraInfo);

         [StructLayout(LayoutKind.Sequential)]
         private struct KBDLLHOOKSTRUCT
         {
             public uint vkCode;
             public uint scanCode;
             public uint flags;
             public uint time;
             public IntPtr dwExtraInfo;
         }

         private enum ActionType
         {
             Mouse,
             Keyboard
         }

         private class InputAction
         {
             public ActionType Type { get; }
             public int KeyCode { get; }
             public TimeSpan Time { get; }

             public InputAction(ActionType type, int keyCode, TimeSpan time)
             {
                 Type = type;
                 KeyCode = keyCode;
                 Time = time;
             }
         }

         #endregion
    }

}
//*/