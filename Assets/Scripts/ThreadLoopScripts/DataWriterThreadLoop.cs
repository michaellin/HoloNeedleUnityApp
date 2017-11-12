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
using System.IO;

using UnityEngine;

namespace thrThreadLoop
{
    public class DataWriterThreadLoop : ThreadLoop
    {
        
        private string _Name;

        private string _rawData;
        private string _fileName;
        private bool _writePending;

        private double _MaximumLoopSpan;
        private bool _CloseWheneverPossible = false;

        public bool isBusy;

        public DataWriterThreadLoop(string strThreadName, Boolean bolIsBackGround, System.Threading.ThreadPriority ePriority, Double numTimerInterval, Double numMaximumLoopSpan,
            string filename)
            : base(strThreadName, bolIsBackGround, ePriority, numTimerInterval)
        {
            this._Name = strThreadName;
            this._MaximumLoopSpan = numMaximumLoopSpan;
            this._fileName = filename;
            this.isBusy = false;
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
                this.Close();
                return;
            }
            if (this._writePending)
            {
                File.AppendAllText(_fileName, _rawData);
                this._writePending = false;
                this.isBusy = false;
            }
        }

        public void writeData(string rawData)
        {
            this._rawData = rawData;
            this.isBusy = true;
            this._writePending = true;
        }
    }
}