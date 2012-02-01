﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="StyleCopStageProcess.cs" company="http://stylecop.codeplex.com">
//   MS-PL
// </copyright>
// <license>
//   This source code is subject to terms and conditions of the Microsoft 
//   Public License. A copy of the license can be found in the License.html 
//   file at the root of this distribution. If you cannot locate the  
//   Microsoft Public License, please send an email to dlr@microsoft.com. 
//   By using this source code in any fashion, you are agreeing to be bound 
//   by the terms of the Microsoft Public License. You must not remove this 
//   notice, or any other, from this software.
// </license>
// <summary>
//   Stage Process that execute the Microsoft StyleCop against the
//   specified file.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace StyleCop.ReSharper611.Core
{
    #region Using Directives

    using System;
    using System.Diagnostics;
    using System.Linq;

    using JetBrains.Application.Progress;
    using JetBrains.Application.Settings;
    using JetBrains.ReSharper.Daemon;
    using JetBrains.ReSharper.Daemon.Stages;
    using JetBrains.ReSharper.Psi;
    using JetBrains.ReSharper.Psi.CSharp;
    using JetBrains.ReSharper.Psi.Tree;

    using StyleCop.Diagnostics;
    using StyleCop.ReSharper611.Options;

    #endregion

    /// <summary>
    /// Stage Process that execute the Microsoft StyleCop against the
    /// specified file.
    /// </summary>
    /// <remarks>
    /// This type is created and executed every time a .cs file is modified in the IDE.
    /// </remarks>
    public class StyleCopStageProcess : IDaemonStageProcess
    {
        #region Constants and Fields

        /// <summary>
        /// Defines the max performance value - this is used to reverse the settings.
        /// </summary>
        private const int MaxPerformanceValue = 9;
        
        /// <summary>
        /// Allows us to run the StyleCop analysers.
        /// </summary>
        private static readonly StyleCopRunnerInt styleCopRunnerInternal = new StyleCopRunnerInt();

        /// <summary>
        /// The process we were started with.
        /// </summary>
        private readonly IDaemonProcess daemonProcess;

        /// <summary>
        /// THe settings store we were construcuted with.
        /// </summary>
        private readonly IContextBoundSettingsStore settingsStore;
        
        /// <summary>
        /// Used to reduce the number of calls to StyleCop to help with performance.
        /// </summary>
        private static Stopwatch performanceStopWatch;

        /// <summary>
        /// Gets set to true after our first run.
        /// </summary>
        private static bool runOnce = false;

        #endregion
        
        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the StyleCopStageProcess class, using the specified
        /// <see cref="IDaemonProcess"/>.
        /// </summary>
        /// <param name="daemonProcess">
        /// <see cref="IDaemonProcess"/>to execute within.
        /// </param>
        /// <param name="settingsStore">
        /// Our settings.
        /// </param>
        public StyleCopStageProcess(IDaemonProcess daemonProcess, IContextBoundSettingsStore settingsStore)
        {
            StyleCopTrace.In(daemonProcess);
            
            this.daemonProcess = daemonProcess;
            this.settingsStore = settingsStore;
            
            InitialiseTimers();

            StyleCopTrace.Out();
        }

        #endregion

        /// <summary>
        /// Gets the Daemon Process.
        /// </summary>
        public IDaemonProcess DaemonProcess
        {
            get
            {
                return this.daemonProcess;
            }
        }
        
        #region Implemented Interfaces

        #region IDaemonStageProcess

        /// <summary>
        /// The execute.
        /// </summary>
        /// <param name="commiter">
        /// The commiter.
        /// </param>
        public void Execute(Action<DaemonStageResult> commiter)
        {
            StyleCopTrace.In();

            if (this.daemonProcess == null)
            {
                return;
            }

            if (this.daemonProcess.InterruptFlag)
            {
                return;
            }

            // inverse the performance value - to ensure that "more resources" actually evaluates to a lower number
            // whereas "less resources" actually evaluates to a higher number. If Performance is set to max, then execute as normal.
             int parsingPerformance = this.settingsStore.GetValue((StyleCopOptionsSettingsKey key) => key.ParsingPerformance);

            var alwaysExecute = parsingPerformance == StyleCopStageProcess.MaxPerformanceValue;

            bool enoughTimeGoneByToExecuteNow = false;

            if (!alwaysExecute)
            {
                enoughTimeGoneByToExecuteNow = performanceStopWatch.Elapsed > new TimeSpan(0, 0, 0, StyleCopStageProcess.MaxPerformanceValue - parsingPerformance);
            }

            if (!alwaysExecute && !enoughTimeGoneByToExecuteNow && runOnce)
            {
                StyleCopTrace.Info("Not enough time gone by to execute.");
                StyleCopTrace.Out();
                return;
            }
            
            runOnce = true;

            styleCopRunnerInternal.Execute(this.daemonProcess.SourceFile.ToProjectFile(), this.daemonProcess.Document);

            var violations =
                (from info in styleCopRunnerInternal.ViolationHighlights let range = info.Range let highlighting = info.Highlighting select new HighlightingInfo(range, highlighting)).ToList();

            commiter(new DaemonStageResult(violations));

            ResetPerformanceStopWatch();

            StyleCopTrace.Out();
        }

        #endregion

        #endregion

        #region Methods

        /// <summary>
        /// Initialises the static timers used to regulate performance of execution of StyleCop analysis.
        /// </summary>
        private static void InitialiseTimers()
        {
            if (performanceStopWatch == null)
            {
                performanceStopWatch = Stopwatch.StartNew();
                performanceStopWatch.Start();
            }
        }

        /// <summary>
        /// Resets the Performance Stopwatch.
        /// </summary>
        private static void ResetPerformanceStopWatch()
        {
            performanceStopWatch.Reset();
            performanceStopWatch.Start();
        }
        
        #endregion
    }
}