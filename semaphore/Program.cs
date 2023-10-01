// This file is part of Silk.NET.
//
// You may modify and distribute Silk.NET under the terms
// of the MIT license. See the LICENSE file for details.

using System.Diagnostics;

using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using Silk.NET.Input;
using Silk.NET.OpenGL.Extensions.ImGui;

using ImGuiNET;

namespace SemaphoreExample
{
    class Program
    {
        private static readonly Semaphore semaphore = new Semaphore(initialCount: 0, maximumCount: 10);
        private static int count = 0;

        private static readonly object locker = new object();

        private static readonly Dictionary<int, float> progress = new Dictionary<int, float>();

        public static void DoWork(object duration)
        {
            semaphore.WaitOne();
            lock(locker) {
                --count;
            }
            // Thread.Sleep((int)((TimeSpan)duration).TotalMilliseconds);
            var sw = new Stopwatch();
            sw.Start();
            while (sw.ElapsedMilliseconds < ((TimeSpan)duration).TotalMilliseconds) {
                // busy wait
                var r = (float)((double)sw.ElapsedMilliseconds / ((TimeSpan)duration).TotalMilliseconds);
                lock(progress) {
                    progress[Thread.CurrentThread.ManagedThreadId] = r;
                }
                Thread.Sleep(10);
            }
            // semaphore.Release();
            // lock(locker) {
            //     ++count;
            // }
        }

        unsafe static void Main(string[] args)
        {
            // Create a Silk.NET window
            using var window = Window.Create(WindowOptions.Default);

            // these must be initialized after we have a window (in Load)
            IInputContext inputContext = null;
            GL gl = null;
            ImGuiController controller = null;
     
            var threads = new List<Tuple<Thread, TimeSpan>>();

            // Our loading function
            window.Load += () =>
            {
                gl = window.CreateOpenGL();
                inputContext = window.CreateInput();
                controller = new ImGuiController(gl, window, inputContext);
                // var io = new ImGuiNET.ImGuiIO();
                var io = ImGui.GetIO();
                io.ConfigWindowsMoveFromTitleBarOnly = true;
            };

            // Handle resizes
            window.FramebufferResize += s =>
            {
                // Adjust the viewport to the new window size
                gl.Viewport(s);
            };
            
            int duration = 1; // duration in seconds
            float progress_ = 0;

            // The render function
            window.Render += delta =>
            {
                // Make sure ImGui is up-to-date
                controller.Update((float)delta);

                // This is where you'll do any rendering beneath the ImGui context
                // Here, we just have a blank screen.
                gl.ClearColor(System.Drawing.Color.FromArgb(255, (int)(.45f * 255), (int)(.55f * 255), (int)(.60f * 255)));
                gl.Clear((uint)ClearBufferMask.ColorBufferBit);

                // This is where you'll do all of your ImGUi rendering
                // Here, we're just showing the ImGui built-in demo window.
                ImGui.ShowMetricsWindow();

                ImGui.Begin("Semaphore");
                ImGui.Text($"Count: {count}");
                ImGui.SameLine();
                if (ImGui.Button("Release")) {
                    semaphore.Release();
                    ++count;
                }

                ImGui.Begin("Task control");
                ImGui.InputInt("Duration", ref duration);
                ImGui.SameLine();

                var now = DateTime.Now;

                if (ImGui.Button("Create task")) {
                    var thr = new Thread(new ParameterizedThreadStart(DoWork));
                    var dur = TimeSpan.FromSeconds(duration);
                    threads.Add(Tuple.Create(thr, dur));
                }

                var header = new[] { "Thread ID", "IsBackground", "IsThreadPoolThread", "IsAlive", "ThreadState", "Actions", "Progress" };
                var plabels = new string[threads.Count];

                ImGui.Begin("Threads");
                if (ImGui.BeginTable("table", header.Length)) {
                    ImGui.TableNextRow();
                    for (var i = 0; i < header.Length; ++i) {
                        ImGui.TableNextColumn();
                        ImGui.Text(header[i]);
                    }
                     ImGui.TableNextRow();
                    for (var i = 0; i < header.Length; ++i) {
                        ImGui.TableNextColumn();
                        ImGui.Separator();
                    }

                    for (var i = 0; i < threads.Count; ++i)
                    {
                        var (t, d) = threads[i];
                        progress.TryGetValue(t.ManagedThreadId, out float p);

                        ImGui.TableNextRow();
                        ImGui.TableNextColumn(); ImGui.Text($"{t.ManagedThreadId}");
                        ImGui.TableNextColumn(); ImGui.Text($"{(t.IsAlive ? t.IsBackground : "")}");
                        ImGui.TableNextColumn(); ImGui.Text($"{t.IsThreadPoolThread}");
                        ImGui.TableNextColumn(); ImGui.Text($"{t.IsAlive}");
                        ImGui.TableNextColumn(); ImGui.Text($"{(t.IsAlive ? t.ThreadState : "" )}");
                        ImGui.TableNextColumn(); if (ImGui.Button($"Start {t.ManagedThreadId}")) { t.Start(d); }
                        ImGui.TableNextColumn(); ImGui.Text($"{p * 100:F0}%%");
                    }
                    ImGui.EndTable();
                }

                ImGui.End();
                // Make sure ImGui renders too!
                controller.Render();
            };

            // The closing function
            window.Closing += () =>
            {
                // Dispose our controller first
                controller?.Dispose();

                // Dispose the input context
                inputContext?.Dispose();

                // Unload OpenGL
                gl?.Dispose();
            };

            // Now that everything's defined, let's run this bad boy!
            window.Run();
            window.Dispose();
        }
    }
}
