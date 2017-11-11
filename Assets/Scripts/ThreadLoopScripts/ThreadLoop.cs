/// <summary>
/// Handles threading. Create ThreadLoop objects for
/// each thread as needed.
/// 
/// Author: A. Siu
/// June 30, 2017
/// </summary>
/// 
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Timers;

using UnityEngine;

namespace thrThreadLoop
{
    public class ThreadLoop
    {

        private bool debug = false;

        public ThreadLoop(string strThreadName, Boolean bolIsBackGround, System.Threading.ThreadPriority ePriority, Double numTimerInterval)
        {
            this._Name = strThreadName;
            this._IsBackGround = bolIsBackGround;
            this._Priority = ePriority;
            this._TimerInterval = numTimerInterval;
        }

        private Thread _Thread;
        private string _Name;
        private Boolean _IsBackGround;
        private System.Threading.ThreadPriority _Priority;
        private Double _TimerInterval;
        private System.Timers.Timer _Timer;

        public void Start()
        {
            ThreadStart tm = new ThreadStart(this.ThreadDelegate);
            this._Thread = new Thread(tm);
            this._Thread.Name = this._Name;
            this._Thread.IsBackground = this._IsBackGround;
            this._Thread.Priority = this._Priority;
            this._Thread.Start();
        }
        private void ThreadDelegate()
        {
            this._Timer = new System.Timers.Timer(this._TimerInterval);
            this._Timer.Elapsed += new ElapsedEventHandler(this.TimerDelegate);
            this._Timer.Enabled = true;
        }
        private void TimerDelegate(System.Object sender, System.Timers.ElapsedEventArgs e)
        {
            if (this._CloseThread)
            {
                this._Timer.Stop();
                this._Timer.Close();
                this._Timer.Dispose();
                this._Timer = null;
                this._Thread.Abort();
                this._Thread = null;
                return;
            }
            this.HandleThreadLoop();
        }

        internal void HandleThreadLoop()
        {
            if (this._IsLoopPaused)
            {
                // if you are here is because the process is paused, 
                // that means we should wait longer before starting the next one
                this.ReportIsPaused();
                this.DoPausedProcess();
                return;
            }
            else if (!Monitor.TryEnter(this))
            {
                // Monitor.Exit(Me) don't need the Monitor.Exit(ME) because if you 
                // are here is because Monitor.TryEnter(Me) returned false and a 
                // lock was not entered if you are here is because the previous 
                // loop was busy, that means we should wait longer before 
                // starting the next one
                this.ReportIsBusy();
                this.DoBusyProcess();
                return;
            }

            this._LastLoopStartDateTime = DateTime.Now;
            this.IncreaseThreadLoopsCounter();
            this.ReportIsRunning();

            //do normal processing
            try
            {
                ///'''''''''''''''''''''''''''''
                this.DoMainProcess();
                ///'''''''''''''''''''''''''''''
                if (debug)
                {
                    Debug.Log("Did Main Process");
                }
            }
            catch (Exception ex)
            {
                //Debug.Log("error at this.DoMainProcess(): " + ex.Message.ToString());
            }
            finally
            {
                Monitor.Exit(this);
            }
            //do post processing
            try
            {
                this.DoPostProcess();
            }
            catch (Exception ex)
            {
                //Debug.Log("error at this.DoMainProcess(): " + ex.Message.ToString());
            }
        }

        private bool _CloseThread = false;
        public bool IsClosed
        {
            get { return this._CloseThread; }
        }
        public void Close()
        {
            this._CloseThread = true;
            Debug.Log("thread closed");
        }

        private bool _IsLoopPaused = false;
        public bool IsPaused
        {
            get { return this._IsLoopPaused; }
        }
        public void Pause()
        {
            this._IsLoopPaused = true;
        }
        public void Resume()
        {
            this._IsLoopPaused = false;
        }

        private DateTime _LastLoopStartDateTime = new DateTime(0);
        public TimeSpan LoopSpan()
        {
            return DateTime.Now.Subtract(this._LastLoopStartDateTime);
        }

        private DateTime _LastLoopLastLoopActivity = new DateTime(0);
        protected virtual void UpdateLastLoopActivity()
        {
            this._LastLoopLastLoopActivity = DateTime.Now;
            this.SetNextNoDataTimeOutDateTime();
        }
        protected virtual void NoLoopActivityProcess()
        {
        }
        protected virtual bool NoDataTimeOutCondition()
        {
            if (this._NoDataTimeOutInterval == 0) return false;
            return (this._NextNoDataTimeOutDateTime < DateTime.Now);
        }
        protected virtual void ReportNoDataTimeOut()
        {
            if (debug)
            {
                Debug.Log("ReportNoDataTimeOut");
            }
        }
        private long _NoDataTimeOutInterval = 0;
        private DateTime _NextNoDataTimeOutDateTime = new DateTime(0);
        protected void SetNextNoDataTimeOutDateTime()
        {
            if (this._NoDataTimeOutInterval == 0) return;
            this._NextNoDataTimeOutDateTime = DateTime.Now.AddMinutes(this._NoDataTimeOutInterval);
        }

        protected virtual void ReportIsPaused()
        {
            if (debug)
            {
                Debug.Log("ReportIsPaused");
            }
        }
        protected virtual void DoPausedProcess()
        {
            if (debug)
            {
                Debug.Log("DoPausedProcess");
            }
        }
        protected virtual void ReportIsBusy()
        {
            if (debug)
            {
                Debug.Log("ReportIsBusy");
            }
        }
        protected virtual void DoBusyProcess()
        {
            if (debug)
            {
                Debug.Log("DoBusyProcess");
            }
        }
        protected virtual void IncreaseThreadLoopsCounter()
        {
            if (debug)
            {
                Debug.Log("IncreaseThreadLoopsCounter");
            }
        }
        protected virtual void ReportIsRunning()
        {
            if (debug)
            {
                Debug.Log("ReportIsRunning");
            }
        }
        protected virtual void DoMainProcess()
        {
            if (debug)
            {
                Debug.Log("DoMainProcess");
            }
        }
        protected virtual void DoPostProcess()
        {
            if (debug)
            {
                Debug.Log("DoPostProcess");
            }
        }

        protected virtual bool BugResetCondition()
        {
            if (this._ResetInterval == 0) return false;
            return (this._NextBugResetDateTime < DateTime.Now);
        }
        protected virtual void ReportResetBug()
        {
            if (debug)
            {
                Debug.Log("ReportResetBug");
            }
        }

        // Set _ResetInterval if you need to reset your program
        // after a certain period of time
        private long _ResetInterval = 0;
        private DateTime _NextBugResetDateTime = new DateTime(0);
        protected void SetNextBugResetDateTime()
        {
            if (this._ResetInterval == 0) return;
            this._NextBugResetDateTime = DateTime.Now.AddMinutes(this._ResetInterval);
        }
    }
}