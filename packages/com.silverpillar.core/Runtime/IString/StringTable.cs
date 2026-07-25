using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace SilverPillar.Core
{
    public class StringTable : SerializedMonoBehaviour
    {
        [Serializable]
        public struct Data
        {
            [OdinSerialize, ShowInInspector]
            private IString m_ColumnName;

            [OdinSerialize, ShowInInspector]
            private IString m_HowToCalculateData;

            public bool SetGameObject(GameObject gameObject)
            {
                bool allGood = m_ColumnName != null &&
                               m_ColumnName.SetGameObject(gameObject);

                allGood &= m_HowToCalculateData != null &&
                           m_HowToCalculateData.SetGameObject(gameObject);

                return allGood;
            }

            public string CalculateColumnName()
            {
                return m_ColumnName != null
                    ? m_ColumnName.CalculateString()
                    : string.Empty;
            }

            public string CalculateData()
            {
                return m_HowToCalculateData != null
                    ? m_HowToCalculateData.CalculateString()
                    : string.Empty;
            }
        }

        private enum SaveType
        {
            OverrideFileForEachSave,
            MakeNewFileForEachSave
        }

        private enum WhenToAutoSave
        {
            DontAutoSave,
            OnDisable,
            OnDestroy
        }

        [Title("Save File Settings")]

        [OdinSerialize, ShowInInspector]
        private IString m_FileName;

        [SerializeField, Tooltip("Will use Application.persistentDataPath")]
        private bool m_DefaultFileLocation = true;

        [OdinSerialize, HideIf(nameof(m_DefaultFileLocation)), ShowInInspector]
        private IString m_FileLocation;

        [SerializeField]
        private SaveType m_SaveType;

        [SerializeField]
        private WhenToAutoSave m_WhenToAutoSave;

        [Title("Table Data")]

        [OdinSerialize, ShowInInspector]
        private List<Data> m_TableData;

        [SerializeField]
        private bool m_ShowData;

        [ShowIf(nameof(m_ShowData)), ReadOnly]
        private List<string> m_RowData;

        [ShowInInspector, ReadOnly]
        private string m_LastSavedFilePath;

        private bool m_Initialized;

        private void OnDisable()
        {
            if (m_WhenToAutoSave == WhenToAutoSave.OnDisable)
            {
                Save();
            }
        }

        private void OnDestroy()
        {
            if (m_WhenToAutoSave == WhenToAutoSave.OnDestroy)
            {
                Save();
            }
        }

        private void Initialize()
        {
            if (m_Initialized)
                return;

            m_RowData ??= new List<string>();
            m_TableData ??= new List<Data>();

            m_RowData.Clear();

            m_FileName?.SetGameObject(gameObject);

            if (!m_DefaultFileLocation)
                m_FileLocation?.SetGameObject(gameObject);

            for (int i = 0; i < m_TableData.Count; i++)
            {
                m_TableData[i].SetGameObject(gameObject);
            }

            // The first row is always the header row.
            m_RowData.Add(CalculateHeaderRow());

            m_Initialized = true;
        }

        /// <summary>
        /// Adds a new row to the table.
        /// </summary>
        public void CreateNewRow(bool calculateValues)
        {
            Initialize();

            string row = calculateValues
                ? CalculateStringData()
                : CalculateEmptyRow();

            m_RowData.Add(row);
        }

        /// <summary>
        /// Recalculates the most recently created data row.
        /// The header row is never recalculated.
        /// </summary>
        public void RecalculateLastRow()
        {
            Initialize();

            if (m_RowData.Count <= 1)
            {
                Debug.LogWarning(
                    $"{nameof(StringTable)} on {gameObject.name} has no data row to recalculate.",
                    this
                );

                return;
            }

            m_RowData[m_RowData.Count - 1] = CalculateStringData();
        }

        private string CalculateHeaderRow()
        {
            if (m_TableData.Count == 0)
                return string.Empty;

            string[] columnNames = new string[m_TableData.Count];

            for (int i = 0; i < m_TableData.Count; i++)
            {
                columnNames[i] = EscapeCsvValue(
                    m_TableData[i].CalculateColumnName()
                );
            }

            return string.Join(",", columnNames);
        }

        private string CalculateStringData()
        {
            if (m_TableData.Count == 0)
                return string.Empty;

            string[] values = new string[m_TableData.Count];

            for (int i = 0; i < m_TableData.Count; i++)
            {
                values[i] = EscapeCsvValue(
                    m_TableData[i].CalculateData()
                );
            }

            return string.Join(",", values);
        }

        private string CalculateEmptyRow()
        {
            if (m_TableData.Count == 0)
                return string.Empty;

            // Creates the correct number of empty CSV columns.
            return string.Join(",", new string[m_TableData.Count]);
        }

        /// <summary>
        /// Saves the current table to a CSV file.
        /// </summary>
        public void Save()
        {
            Initialize();

            try
            {
                string directoryPath = CalculateDirectoryPath();
                string fileName = CalculateFileName();

                Directory.CreateDirectory(directoryPath);

                switch (m_SaveType)
                {
                    case SaveType.OverrideFileForEachSave:
                        m_LastSavedFilePath = Path.Combine(
                            directoryPath,
                            fileName + ".csv"
                        );
                        break;

                    case SaveType.MakeNewFileForEachSave:
                        m_LastSavedFilePath = GetNextNumberedFilePath(
                            directoryPath,
                            fileName
                        );
                        break;

                    default:
                        throw new ArgumentOutOfRangeException();
                }

                File.WriteAllLines(
                    m_LastSavedFilePath,
                    m_RowData.ToArray(),
                    new UTF8Encoding(false)
                );

                Debug.Log(
                    $"CSV file saved successfully at:\n{m_LastSavedFilePath}",
                    this
                );
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"Failed to save CSV file for {nameof(StringTable)} " +
                    $"on {gameObject.name}.\n{exception}",
                    this
                );
            }
        }

        private static string GetNextNumberedFilePath(
            string directoryPath,
            string fileName)
        {
            int fileNumber = 1;
            string filePath;

            do
            {
                filePath = Path.Combine(
                    directoryPath,
                    $"{fileName}_{fileNumber}.csv"
                );

                fileNumber++;
            }
            while (File.Exists(filePath));

            return filePath;
        }

        private string CalculateDirectoryPath()
        {
            if (m_DefaultFileLocation)
                return Application.persistentDataPath;

            string customLocation = m_FileLocation != null
                ? m_FileLocation.CalculateString()
                : string.Empty;

            if (string.IsNullOrWhiteSpace(customLocation))
            {
                Debug.LogWarning(
                    "The custom CSV file location was empty. " +
                    "Application.persistentDataPath will be used instead.",
                    this
                );

                return Application.persistentDataPath;
            }

            return customLocation.Trim();
        }

        private string CalculateFileName()
        {
            string fileName = m_FileName != null
                ? m_FileName.CalculateString()
                : string.Empty;

            if (string.IsNullOrWhiteSpace(fileName))
                fileName = gameObject.name + "_StringTable";

            // Remove any directory supplied through the file-name field.
            fileName = Path.GetFileNameWithoutExtension(fileName.Trim());

            foreach (char invalidCharacter in Path.GetInvalidFileNameChars())
            {
                fileName = fileName.Replace(
                    invalidCharacter,
                    '_'
                );
            }

            return string.IsNullOrWhiteSpace(fileName)
                ? "StringTable"
                : fileName;
        }

        /// <summary>
        /// Escapes a value according to CSV formatting rules.
        /// </summary>
        private static string EscapeCsvValue(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            bool requiresQuotationMarks =
                value.Contains(",") ||
                value.Contains("\"") ||
                value.Contains("\r") ||
                value.Contains("\n");

            if (!requiresQuotationMarks)
                return value;

            string escapedValue = value.Replace("\"", "\"\"");

            return $"\"{escapedValue}\"";
        }
    }
}