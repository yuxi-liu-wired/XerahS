#region License Information (GPL v3)

/*
    XerahS - The Avalonia UI implementation of ShareX
    Copyright (c) 2007-2026 ShareX Team

    This program is free software; you can redistribute it and/or
    modify it under the terms of the GNU General Public License
    as published by the Free Software Foundation; either version 2
    of the License, or (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program; if not, write to the Free Software
    Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.

    Optionally you can also view the license at <http://www.gnu.org/licenses/>.
*/

#endregion License Information (GPL v3)

using System.Reflection;
using NUnit.Framework;
using XerahS.Core;
using XerahS.Core.Hotkeys;
using XerahS.Core.Tasks.Processors;
using XerahS.RegionCapture.ScreenRecording;
using XerahS.Tests.Xip0052;
using XerahS.UI.Services;
using HotkeyInfo = XerahS.Platform.Abstractions.HotkeyInfo;

namespace XerahS.Tests.Services;

[TestFixture]
public class WorkflowOrchestratorTests
{
    [Test]
    public void PauseRecordingHotkey_UsesInjectedCoordinator()
    {
        var taskManager = new FakeDesktopTaskManager();
        var coordinator = new FakeScreenRecordingCoordinator { IsRecording = true };
        var orchestrator = new WorkflowOrchestrator(taskManager, coordinator);

        InvokeHotkey(orchestrator, WorkflowType.PauseScreenRecording);

        Assert.That(coordinator.TogglePauseResumeCalls, Is.EqualTo(1));
    }

    [Test]
    public void PauseRecordingHotkey_DoesNotToggleWhenCoordinatorDisablesPause()
    {
        var taskManager = new FakeDesktopTaskManager();
        var coordinator = new FakeScreenRecordingCoordinator
        {
            IsRecording = true,
            CurrentCapabilities = RecordingRuntimeCapabilities.None
        };

        var orchestrator = new WorkflowOrchestrator(taskManager, coordinator);

        InvokeHotkey(orchestrator, WorkflowType.PauseScreenRecording);

        Assert.That(coordinator.TogglePauseResumeCalls, Is.EqualTo(0));
    }

    [Test]
    public void AbortRecordingHotkey_UsesInjectedCoordinator()
    {
        var taskManager = new FakeDesktopTaskManager();
        var coordinator = new FakeScreenRecordingCoordinator { IsPaused = true };
        var orchestrator = new WorkflowOrchestrator(taskManager, coordinator);

        InvokeHotkey(orchestrator, WorkflowType.AbortScreenRecording);

        Assert.That(coordinator.AbortCalls, Is.EqualTo(1));
    }

    [Test]
    public void RecordingHotkeyDuringActiveRecording_SignalsInjectedCoordinator()
    {
        var taskManager = new FakeDesktopTaskManager();
        var coordinator = new FakeScreenRecordingCoordinator { IsRecording = true };
        var orchestrator = new WorkflowOrchestrator(taskManager, coordinator);

        InvokeHotkey(orchestrator, WorkflowType.ScreenRecorder);

        Assert.That(coordinator.SignalStopCalls, Is.EqualTo(1));
    }

    [Test]
    public void QuickActionCompletionNotification_IsSuppressed()
    {
        var info = new TaskInfo
        {
            SuppressCompletionNotification = true
        };

        Assert.That(WorkflowOrchestrator.ShouldShowCompletionNotification(info), Is.False);
    }

    [Test]
    public void StandardCompletionNotification_UsesWorkflowSetting()
    {
        var info = new TaskInfo();
        info.TaskSettings.GeneralSettings.ShowToastNotificationAfterTaskCompleted = true;

        Assert.That(WorkflowOrchestrator.ShouldShowCompletionNotification(info), Is.True);

        info.TaskSettings.GeneralSettings.ShowToastNotificationAfterTaskCompleted = false;
        Assert.That(WorkflowOrchestrator.ShouldShowCompletionNotification(info), Is.False);
    }

    [Test]
    public void QuickActionTasks_PreserveAfterCaptureWindowForFutureRuns()
    {
        var result = (
            AfterCaptureTasks.CopyImageToClipboard,
            AfterUploadTasks.None,
            false,
            AfterCaptureQuickAction.CopyImage);

        var tasks = CaptureJobProcessor.GetAfterCaptureTasksForRun(result);

        Assert.That(
            tasks,
            Is.EqualTo(AfterCaptureTasks.ShowAfterCaptureWindow | AfterCaptureTasks.CopyImageToClipboard));
    }

    [Test]
    public void ContinueTasks_UseTheDialogSelectionUnchanged()
    {
        var selected = AfterCaptureTasks.ShowAfterCaptureWindow | AfterCaptureTasks.SaveImageToFile;
        var result = (selected, AfterUploadTasks.None, false, AfterCaptureQuickAction.None);

        var tasks = CaptureJobProcessor.GetAfterCaptureTasksForRun(result);

        Assert.That(tasks, Is.EqualTo(selected));
    }

    private static void InvokeHotkey(WorkflowOrchestrator orchestrator, WorkflowType workflowType)
    {
        var method = typeof(WorkflowOrchestrator).GetMethod("HotkeyManager_HotkeyTriggered", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Could not find hotkey handler.");

        var workflow = new WorkflowSettings(workflowType, new HotkeyInfo())
        {
            TaskSettings = new TaskSettings
            {
                Job = workflowType
            }
        };

        method.Invoke(orchestrator, new object?[] { null, workflow });
    }
}
