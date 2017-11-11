﻿/// <summary>
/// Handles data streaming threads. Create DataStreamThreadLoop objects 
/// for each thread that requires any data communication.
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

using UnityEngine;

namespace thrThreadLoop
{
    public class DataQueueThreadLoop : ThreadLoop
    {

        private string _Name;

        private DataWriterThreadLoop __DataWriterThread;

        private Queue<string> DataToWrite = new Queue<string>();

        private double _MaximumLoopSpan;
        private bool _CloseWheneverPossible = false;

        public DataQueueThreadLoop(string strThreadName, Boolean bolIsBackGround, System.Threading.ThreadPriority ePriority, Double numTimerInterval, Double numMaximumLoopSpan)
            : base(strThreadName, bolIsBackGround, ePriority, numTimerInterval)
        {
            this._Name = strThreadName;
            this._MaximumLoopSpan = numMaximumLoopSpan;
        }

        /// <summary>
        /// Call to kill thread immediately.
        /// </summary>
        public void CloseAtConvenience()
        {
            this._CloseWheneverPossible = true;
        }

        protected override void DoMainProcess()
        {
            if (this._CloseWheneverPossible)
            {
                Debug.Log("closing");
                this.CloseStream();
                this.Close();
                return;
            }

            bool bolLoopActivity = false;
            bool bolDidOne = true;
            byte[] aBytes;
            while (bolDidOne && this.LoopSpan().TotalMilliseconds < this._MaximumLoopSpan)
            {
                bolDidOne = false;
                aBytes = this.GetBytesToWrite();
                if ((aBytes != null) && aBytes.Length > 0)
                {
                    bolDidOne = true;
                    this.WriteDataStream(aBytes);
                }

                if (this._CloseWheneverPossible)
                {
                    this.CloseStream();
                    this.Close();
                    return;
                }

                if (this.ReadDataStream())
                {
                    bolDidOne = true;
                }

                if (this._CloseWheneverPossible)
                {
                    this.CloseStream();
                    this.Close();
                    return;
                }

                if (bolDidOne) bolLoopActivity = true;
            }
            if (bolLoopActivity)
            {
                this.UpdateLastLoopActivity();
            }
            else
            {
                this.NoLoopActivityProcess();
            }
            //this.ResetProcess();
        }

        /// <summary>
        /// ResetProcess used when thread
        /// </summary>
        private void ResetProcess()
        {
            if (this.BugResetCondition())
            {
                //the low level RS code has a bug that need to be reset every once in a while
                //because otherwise it consumes the CPU and the program gets stuck
                this.ReportResetBug();
                this.SetNextBugResetDateTime();
                this.CloseStream();
            }
            else if (this.NoDataTimeOutCondition())
            {
                //if it's been a while without receiving a data, close it and the try to open next
                this.ReportNoDataTimeOut();
                this.SetNextNoDataTimeOutDateTime();
                this.CloseStream();
            }
            if (!this.IsOpenStream())
            {
                //try to re-open the conection
                this.OpenStream();
            }
        }

        /// <summary>
        /// Enqueues the strings to write.
        /// </summary>
        /// <param name="data">string of data.</param>
        public void EnqueueDataToWrite(string data)
        {
            this.DataToWrite.Enqueue(data);
        }

        /// <summary>
        /// Get the next data from the queue to write.
        /// </summary>
        private string GetDataToWrite()
        {
            if (this.DataToWrite.Count == 0) return null;
            return this.DataToWrite.Dequeue();
        }
    }
}