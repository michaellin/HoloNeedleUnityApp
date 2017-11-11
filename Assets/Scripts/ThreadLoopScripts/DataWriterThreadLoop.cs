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
    public class DataWriterThreadLoop : ThreadLoop
    {

        private string _Name;

        private Queue<string> _rawData;

        private double _MaximumLoopSpan;
        private bool _CloseWheneverPossible = false;

        public DataWriterThreadLoop(string strThreadName, Boolean bolIsBackGround, System.Threading.ThreadPriority ePriority, Double numTimerInterval, Double numMaximumLoopSpan,
            string filename, Queue<string> data2Write)
            : base(strThreadName, bolIsBackGround, ePriority, numTimerInterval)
        {
            this._Name = strThreadName;
            this._MaximumLoopSpan = numMaximumLoopSpan;
            this._rawData = data2Write;
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

            File.AppendAllText(filename, _rawData);
        }
    }
}