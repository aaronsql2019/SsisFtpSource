using System;
using System.Data;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SsisFtpSource
{
    public static class HelperClass
    {

        static public DataTable GetDataTable(String propertyValue)
        {
            DataTable dt = new DataTable();
            try
            {
                dt.Columns.Add("ID", Type.GetType("System.String"));
                dt.Columns.Add("SomeDate", Type.GetType("System.DateTime"));
                dt.Columns.Add("SomeText", Type.GetType("System.String"));
                dt.Columns.Add("SomeInt", Type.GetType("System.Int32"));

                for (int i = 0; i < 10; i++ )
                {
                    DataRow dRow = dt.NewRow();
                    dRow["ID"] = "ID" + i.ToString();
                    dRow["SomeDate"] = DateTime.Now;
                    dRow["SomeText"] = "SomeText" + i.ToString();
                    dRow["SomeInt"] = i;
                    dt.Rows.Add(dRow);
                }

                return dt;
            }
            catch (Exception e)
            {
                throw e;
            }

        }
    }
}
