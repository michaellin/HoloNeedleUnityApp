using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
//using RecData;

namespace RecData {

	public class RecordData {

		public string filename;
		public int dataColumns;
		private List<List<string>> data;
        private string textToWrite;

        /// <summary>
        /// Constructor. 
        /// Inputs: file name and number of data columns for this file.
        /// </summary>
        public RecordData (string filename = "", int dataColumns = 1) {
			this.filename = filename;
			this.dataColumns = dataColumns;
            // Here do the directory checking
            data = new List<List<string>> ();
			for (int i = 0; i < dataColumns; i++) {
				data.Add(new List<string> ());
			}
		}

        /// <summary>
        /// Add Data Method. 
        /// Inputs: datapoints as strings. Expects same # as columns.
        /// Ex. if # columns is 3 --> addData("1","2","3") is accepted.
        /// Do it with threads. e.g. every N data kick off a thread to append to file.
        /// </summary>
        public void addData (params string [] datapoint) {
            // here kick off a thread to save data every N data points
            for (int c = 0; c < dataColumns; c++)
            {
                textToWrite = textToWrite + datapoint[c] + ",";
            }
            textToWrite = textToWrite + "\n";
        }
        
		/// <summary>
		/// Write To File Method. 
		/// Outputs data to file in column format.
		/// </summary>
		public void WriteToFile () {

			// Check if data was empty
			if (data.Count == 0) {
				Debug.Log ("Error: Empty data. Could not write");
				return;
			}
				
			int numDatapoints = data [0].Count;

            // convert stored data to string
   //         float startTimeloop = Time.realtimeSinceStartup;
   //         string textToWrite = "";
			//for (int i = 0; i < numDatapoints; i++) {
			//	for (int c = 0; c < dataColumns; c++) {
			//		textToWrite = textToWrite + data [c] [i] + ",";
			//	}
			//	textToWrite += "\n";
			//}
   //         Debug.Log("loop only: " + (Time.realtimeSinceStartup - startTimeloop) + "s");

            // if no file name is given, use default name
            if ( filename == "" ) {
				filename = "default_data";
			} 

			// save file to Data directory
			filename = Directory.GetCurrentDirectory() + "/Assets/Data/" + filename;
            
            // check to see file name is not already in use so
            // we don't override existing data
            string temp = filename + ".txt";
            int k = 0;
            while (File.Exists(temp))
            {
                Debug.Log("File name already in use. Finding an alternative.");
                temp = filename + "_" + k + ".txt";
                k++;
            }

            filename = temp;

            // write to the file
            float startTime = Time.realtimeSinceStartup;
            File.AppendAllText(filename, textToWrite);
            Debug.Log("writing only: " + (Time.realtimeSinceStartup - startTime) + "s");
            Debug.Log( "Saved data to file: " + filename );
		}


        /// <summary>
        /// Write To File Method. 
        /// Outputs data to file assuming a specific name is given.
        /// </summary>
        public void WriteToFileName()
        {
            // Check if data was empty
            if (data.Count == 0)
            {
                Debug.Log("Error: Empty data. Could not write");
                return;
            }

            int numDatapoints = data[0].Count;

            // convert stored data to string
            string textToWrite = "";
            for (int i = 0; i < numDatapoints; i++)
            {
                for (int c = 0; c < dataColumns; c++)
                {
                    textToWrite = textToWrite + data[c][i] + ",";
                    //if (data[c][i].Length < 15)
                    //    textToWrite += "\t";
                    //if (data[c][i].Length < 8)
                    //    textToWrite += "\t";
                }
                textToWrite += "\n";
            }
            
            // save file to Data directory
            //filename = Directory.GetCurrentDirectory() + "/Assets/Data/" + filename;
            
            // write to the file
            File.AppendAllText(filename, textToWrite);
            Debug.Log("Saved data to file: " + filename);
        }
    }
}