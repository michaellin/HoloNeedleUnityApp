/// <summary>
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

        private string _fileName;

        private DataWriterThreadLoop _DataWriterThread;

        private Queue<string> DataToWrite = new Queue<string>();

        private double _MaximumLoopSpan;
        private bool _CloseWheneverPossible = false;

        public DataQueueThreadLoop(string strThreadName, Boolean bolIsBackGround, System.Threading.ThreadPriority ePriority, Double numTimerInterval, Double numMaximumLoopSpan,
            string filename)
            : base(strThreadName, bolIsBackGround, ePriority, numTimerInterval)
        {
            this._Name = strThreadName;
            this._MaximumLoopSpan = numMaximumLoopSpan;
            this._fileName = filename;

            _DataWriterThread = new DataWriterThreadLoop("dataWriterThread", true, System.Threading.ThreadPriority.Normal, 5, 4, _fileName);
            _DataWriterThread.Start();
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
                _DataWriterThread.Close();
                this.Close();
                return;
            }

            if (DataToWrite.Count > 0 && !_DataWriterThread.isBusy)
            {
                _DataWriterThread.writeData(GetDataToWrite());
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