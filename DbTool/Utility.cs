﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;
using System.Text;

namespace DbTool
{
    /// <summary>
    /// 工具类 
    /// </summary>
    public static class Utility
    {
        /// <summary>
        /// 根据数据库表列信息，生成model 
        /// </summary>
        /// <param name="tableEntity"> 表信息 </param>
        /// <param name="modelNamespace"> model class 命名空间 </param>
        /// <param name="prefix"> model class前缀 </param>
        /// <param name="suffix"> model class后缀 </param>
        /// <returns></returns>
        public static string GenerateModelText(this TableEntity tableEntity, string modelNamespace, string prefix, string suffix)
        {
            if (tableEntity == null)
            {
                throw new ArgumentNullException("tableEntity", "表信息不能为空");
            }
            StringBuilder sbText = new StringBuilder();
            sbText.AppendLine("using System;");
            sbText.AppendLine("namespace " + modelNamespace);
            sbText.AppendLine("{");
            if (!String.IsNullOrEmpty(tableEntity.TableDesc))
            {
                sbText.AppendLine("\t/// <summary>" + System.Environment.NewLine + "\t/// " + tableEntity.TableDesc + System.Environment.NewLine + "\t/// </summary>");
            }
            sbText.AppendLine("\tpublic class " + prefix + tableEntity.TableName + suffix);
            sbText.AppendLine("\t{");
            foreach (var item in tableEntity.Columns)
            {
                item.DataType = SqlDbType2FclType(item.DataType, item.IsNullable); //转换为FCL数据类型
                string tmpColName = item.ColumnName.ToPrivateFieldName();
                sbText.AppendLine("\t\tprivate " + item.DataType + " " + tmpColName + ";");
                if (!String.IsNullOrEmpty(item.ColumnDesc))
                {
                    sbText.AppendLine("\t\t/// <summary>" + System.Environment.NewLine + "\t\t/// " + item.ColumnDesc + System.Environment.NewLine + "\t\t/// </summary>");
                }
                sbText.AppendLine("\t\tpublic " + item.DataType + " " + item.ColumnName);
                sbText.AppendLine("\t\t{");
                sbText.AppendLine("\t\t\tget { return " + tmpColName + "; }");
                sbText.AppendLine("\t\t\tset { " + tmpColName + " = value; }");
                sbText.AppendLine("\t\t}");
                sbText.AppendLine();
            }
            sbText.AppendLine("\t}");
            sbText.AppendLine("}");
            return sbText.ToString();
        }

        /// <summary>
        /// 利用反射和泛型将 DataTable 转换为 List 
        /// </summary>
        /// <param name="dt"> DataTable 对象 </param>
        /// <returns> List对象 </returns>
        public static List<T> DataTableToList<T>(this DataTable dt) where T : class, new()
        {
            // 定义集合
            List<T> ts = new List<T>(dt.Rows.Count);
            // 获得此模型的类型
            Type type = typeof(T);
            //定义一个临时变量
            string tempName = string.Empty;
            //遍历DataTable中所有的数据行
            foreach (DataRow dr in dt.Rows)
            {
                T t = new T();
                // 获得此模型的公共属性
                PropertyInfo[] propertys = t.GetType().GetProperties();
                //遍历该对象的所有属性
                foreach (PropertyInfo pi in propertys)
                {
                    tempName = pi.Name;//将属性名称赋值给临时变量
                    //检查DataTable是否包含此列（列名==对象的属性名）
                    if (dt.Columns.Contains(tempName))
                    {
                        //取值
                        object value = dr[tempName];
                        //如果非空，则赋给对象的属性
                        if (value != DBNull.Value)
                        {
                            if (pi.PropertyType == typeof (string) && value.GetType() != typeof(string))
                            {
                                pi.SetValue(t, value.ToString(), null);
                            }
                            else
                            {
                                pi.SetValue(t, value, null);
                            }
                        }
                    }
                }
                //对象添加到泛型集合中
                ts.Add(t);
            }
            return ts;
        }

        /// <summary>
        /// 数据库数据类型转换为FCL类型 
        /// </summary>
        /// <param name="dbType"> 数据库数据类型 </param>
        /// <param name="isNullable"> 该数据列是否可以为空 </param>
        /// <returns></returns>
        public static string SqlDbType2FclType(string dbType, bool isNullable = true)
        {
            DbType sqlDbType = (DbType)Enum.Parse(typeof(DbType), dbType, true);
            string type = null;
            switch (sqlDbType)
            {
                case DbType.Bit:
                    type = isNullable ? "bool?" : "bool";
                    break;

                case DbType.Float:
                case DbType.Real:
                    type = isNullable ? "double?" : "double";
                    break;

                case DbType.Binary:
                case DbType.VarBinary:
                case DbType.Image:
                case DbType.Timestamp:
                case DbType.RowVersion:
                    type = "byte[]";
                    break;

                case DbType.TinyInt:
                    type = isNullable ? "byte?" : "byte";
                    break;

                case DbType.SmallInt:
                case DbType.Int:
                    type = isNullable ? "int?" : "int";
                    break;

                case DbType.BigInt:
                    type = isNullable ? "long?" : "long";
                    break;

                case DbType.Char:
                case DbType.NChar:
                case DbType.NText:
                case DbType.NVarChar:
                case DbType.VarChar:
                case DbType.Text:
                    type = "string";
                    break;

                case DbType.Numeric:
                case DbType.Money:
                case DbType.Decimal:
                case DbType.SmallMoney:
                    type = isNullable ? "decimal?" : "decimal";
                    break;

                case DbType.UniqueIdentifier:
                    type = isNullable ? "Guid?" : "Guid";
                    break;

                case DbType.Date:
                case DbType.SmallDateTime:
                case DbType.DateTime:
                case DbType.DateTime2:
                    type = isNullable ? "DateTime?" : "DateTime";
                    break;

                case DbType.Time:
                    type = isNullable ? "TimeSpan?" : "TimeSpan";
                    break;

                case DbType.DateTimeOffset:
                    type = isNullable ? "DateTimeOffset?" : "DateTimeOffset";
                    break;

                default:
                    type = "object";
                    break;
            }
            return type;
        }

        /// <summary>
        /// 获取数据库类型对应的默认长度
        /// </summary>
        /// <param name="dbType">数据类型</param>
        /// <param name="defaultLength">自定义默认长度</param>
        /// <returns></returns>
        public static int GetDefaultSizeForDbType(string dbType,int defaultLength = 50)
        {
            DbType sqlDbType = (DbType)Enum.Parse(typeof(DbType), dbType, true);
            int len = defaultLength;
            switch (sqlDbType)
            {
                case DbType.BigInt:
                    len = 8;
                    break;
                case DbType.Binary:
                    len = 8;
                    break;
                case DbType.Bit:
                    len = 1;
                    break;
                case DbType.Char:
                    break;
                case DbType.Date:
                case DbType.DateTime:
                case DbType.DateTime2:
                case DbType.DateTimeOffset:
                    len = 8;
                    break;
                case DbType.Decimal:
                    len = 20;
                    break;
                case DbType.Float:
                    len = 20;
                    break;
                case DbType.Image:
                    break;
                case DbType.Int:
                    len = 4;
                    break;
                case DbType.Money:
                    len = 20;
                    break;
                case DbType.NChar:
                    break;
                case DbType.NText:
                    len = 200;
                    break;
                case DbType.Numeric:
                    len = 20;
                    break;
                case DbType.NVarChar:
                    break;
                case DbType.Real:
                    len = 10;
                    break;
                case DbType.RowVersion:
                    break;
                case DbType.SmallDateTime:
                    len = 4;
                    break;
                case DbType.SmallInt:
                    len = 2;
                    break;
                case DbType.SmallMoney:
                    break;
                case DbType.Text:
                    len = 500;
                    break;
                case DbType.Time:
                    len = 8;
                    break;
                case DbType.Timestamp:
                    break;
                case DbType.TinyInt:
                    len = 1;
                    break;
                case DbType.UniqueIdentifier:
                    len = 16;
                    break;
                case DbType.VarBinary:
                    break;
                case DbType.VarChar:
                    break;
                case DbType.Variant:
                    break;
                case DbType.Xml:
                    break;
                case DbType.Structured:
                    break;
                default:
                    break;
            }
            return len;
        }

        /// <summary>
        /// 根据表信息生成sql语句 
        /// </summary>
        /// <param name="tableEntity"> 表信息 </param>
        /// <returns></returns>
        public static string GenerateSqlStatement(this TableEntity tableEntity,bool genDescriotion = true)
        {
            if (String.IsNullOrEmpty(tableEntity.TableName))
            {
                return "";
            }
            StringBuilder sbSqlText = new StringBuilder(), sbSqlDescText = new StringBuilder();
            //create table
            sbSqlText.AppendFormat("CREATE TABLE [{0}].[{1}](", tableEntity.TableSchema,tableEntity.TableName);
            //create description
            if (genDescriotion && !String.IsNullOrEmpty(tableEntity.TableDesc))
            {
                sbSqlDescText.AppendFormat(DbHelper.AddTableDescSqlFormat, tableEntity.TableName, tableEntity.TableDesc);
            }
            if (tableEntity.Columns.Count > 0)
            {
                foreach (var col in tableEntity.Columns)
                {
                    sbSqlText.AppendLine();
                    sbSqlText.AppendFormat("[{0}] {1}", col.ColumnName, col.DataType);
                    if (col.DataType.ToUpperInvariant().Contains("CHAR"))
                    {
                        sbSqlText.AppendFormat("({0})", col.Size.ToString());
                    }
                    if (col.IsPrimaryKey)
                    {
                        sbSqlText.Append(" PRIMARY KEY ");
                    }
                    //Nullable
                    if (!col.IsNullable)
                    {
                        sbSqlText.Append(" NOT NULL ");
                    }
                    //Default Value
                    if (col.DefaultValue != null && !String.IsNullOrEmpty(col.DefaultValue.ToString()))
                    {
                        if (col.IsPrimaryKey && col.DataType.ToUpper().Contains("INT"))
                        {
                            sbSqlText.Append(" IDENTITY(1,1) ");
                        }
                        else
                        {
                            if (col.DataType.ToUpperInvariant().Contains("CHAR") && !col.DefaultValue.ToString().StartsWith("N'"))
                            {
                                sbSqlText.AppendFormat(" DEFAULT(N'{0}')", col.DefaultValue);
                            }
                            else
                            {
                                sbSqlText.AppendFormat(" DEFAULT({0}) ", col.DefaultValue);
                            }
                        }
                    }
                    //
                    sbSqlText.Append(",");
                    //
                    if (genDescriotion && !String.IsNullOrEmpty(col.ColumnDesc))
                    {
                        sbSqlDescText.AppendLine();
                        sbSqlDescText.AppendFormat(DbHelper.AddColumnDescSqlFormat, tableEntity.TableName, col.ColumnName, col.ColumnDesc);
                    }
                }
                sbSqlText.Remove(sbSqlText.Length - 1, 1);
                sbSqlText.AppendLine();
            }
            sbSqlText.AppendLine(");");
            sbSqlText.Append(sbSqlDescText.ToString());
            return sbSqlText.ToString();
        }

        /// <summary>
        /// TrimTableName 
        /// </summary>
        /// <returns></returns>
        public static string TrimTableName(this string tableName)
        {
            if (String.IsNullOrEmpty(tableName))
            {
                return "";
            }
            tableName = tableName.Trim();
            if (tableName.StartsWith("tab") || tableName.StartsWith("tbl"))
            {
                return tableName.Substring(3);
            }
            else if (tableName.StartsWith("tab_") || tableName.StartsWith("tbl_"))
            {
                tableName = tableName.Substring(4);
            }
            return tableName;
        }

        /// <summary>
        /// 将属性名称转换为私有字段名称 
        /// </summary>
        /// <param name="propertyName"> 属性名称 </param>
        /// <returns> 私有字段名称 </returns>
        public static string ToPrivateFieldName(this string propertyName)
        {
            if (String.IsNullOrEmpty(propertyName))
            {
                return "";
            }
            if (propertyName.Equals(propertyName.ToUpperInvariant()))
            {
                return propertyName.ToLowerInvariant();
            }
            if (char.IsUpper(propertyName[0]))//首字母大写，首字母转换为小写
            {
                return char.ToLowerInvariant(propertyName[0]) + propertyName.Substring(1);
            }
            else
            {
                return "_" + propertyName;
            }
        }
    }
}