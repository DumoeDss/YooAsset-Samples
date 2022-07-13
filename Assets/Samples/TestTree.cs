using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class TestTree : MonoBehaviour
{
	public string fileName;

	[ContextMenu("Test")]
	void Test()
	{
		TreeData treeData = new TreeData();
		treeData.LoadData(Application.dataPath.Remove(Application.dataPath.LastIndexOf("/")) + "/" + fileName);
	}

	public class LeaveData
	{
		public string Name;
		public string allLevel;
		public int Level;
		public int Index;
		public LeaveData Parent;
		public List<LeaveData> Leaves;

		public LeaveData(string name)
		{
			Name = name;
			Leaves = new List<LeaveData>();
		}

		public LeaveData(RawData rawData)
		{
			Name = rawData.Name;
			Level = rawData.Level;
			Leaves = new List<LeaveData>();
		}

        public override string ToString()
        {
			string result = Name + allLevel + "\n";
			if (Leaves != null&&Leaves.Count>0)
            {
                for (int i = 0; i < Leaves.Count; i++)
                {
					result+=Leaves[i].ToString();
				}
            }
           
			return result;
		}
    }

	public class RawData
	{
		public string Name;
		public int Level;
		public int Num;
	}

	public class TreeData
	{
		public LeaveData Tree;

		List<string> ReadAllLines(string path)
		{
			List<string> res=new List<string>();
			if (File.Exists(path))
			{
				using (FileStream fileStream = File.OpenRead(path))
				{
					using (StreamReader streamReader = new StreamReader(fileStream))
					{
                        while (!streamReader.EndOfStream)
                        {
							string line = streamReader.ReadLine();
							res.Add(line);
						}
					}
				}
			}
			return res;
		}

		List<RawData> ParseDatas(string path)
		{
			var datas = ReadAllLines(path);
			int level = 0;
			List<RawData> result = new List<RawData>();

			for (int i = 0; i < datas.Count; i++)
			{
				RawData rawData = new RawData();

				if (datas[i].Contains("OCCURS"))
				{
					level++;
					rawData.Name = datas[i].Substring(0, datas[i].IndexOf("OCCURS")).Trim();
					rawData.Num = int.Parse(datas[i].Remove(0, datas[i].IndexOf("OCCURS")).Replace("OCCURS", "").Replace("TIMES.", "").Trim());
					rawData.Level = level;
				}
				else
				{
					rawData.Name = datas[i].Trim();
				}
				result.Add(rawData);
			}
			return result;
		}

		public void UpdateName(LeaveData leaveData, int num)
		{
            if (string.IsNullOrEmpty(leaveData.allLevel))
            {
				leaveData.allLevel = $"-{num+1}";
			}
			else
            {
				leaveData.allLevel = $"-{num + 1}{leaveData.allLevel}";
			}
			if (leaveData.Leaves != null)
			{
				foreach (var leave in leaveData.Leaves)
				{
					if(leave.Leaves!=null&& leave.Leaves.Count > 0)
						UpdateName(leave, num);
				}
			}
		}
		
		public void LoadData(string dataPath)
		{
			var datas = ReadAllLines(dataPath);
			Tree = new LeaveData(datas[0]);

			List<RawData> rawDatas = ParseDatas(dataPath);
			List<LeaveData> lastLeaves = new List<LeaveData>();
			for (int i = rawDatas.Count - 1; i >= 0; i--)
			{
				if (rawDatas[i].Num > 0)
				{
					List<LeaveData> leaves = new List<LeaveData>();
					for (int j = 0; j < rawDatas[i].Num; j++)
					{
						var leave = new LeaveData(rawDatas[i]);
						if (lastLeaves != null && lastLeaves.Count > 0)
						{
							leave.Leaves = new List<LeaveData>(lastLeaves);
						}
						for (int k = 0; k < leave.Leaves.Count; k++)
						{
							UpdateName(leave.Leaves[k], j);
						}
						leaves.Add(leave);
					}
					lastLeaves = leaves;
				}
				else if (i == rawDatas.Count - 1)
				{
					List<LeaveData> leaves = new List<LeaveData>();
					for (int j = 0; j < rawDatas[i-1].Num; j++)
					{
						var leave = new LeaveData(rawDatas[i]);
						if (lastLeaves != null && lastLeaves.Count > 0)
						{
							leave.Leaves = new List<LeaveData>(lastLeaves);
						}
						UpdateName(leave, j);
						leaves.Add(leave);
					}
					lastLeaves = leaves;

				}
				else
				{
					LeaveData leaveData = new LeaveData(rawDatas[i].Name);
					leaveData.Leaves = lastLeaves;
					lastLeaves = new List<LeaveData>() { leaveData };
					if (i == 0)
						Tree = leaveData;
				}
			}

           Debug.Log(Tree.ToString());
		}

	}
}
