using Dapper;
using System.Data.SqlClient;
while (true)
{
    Console.WriteLine("開始請按任意鍵，結束請按 q");
    string q = Console.ReadLine();
    if (q == "q")
    {
        Environment.Exit(0);
    }
    else
    {
        Console.WriteLine("GenerateClassbyDatabase GO!");
        string filePath = "E:/Project/GenerateClassbyDatabase/GenerateClassbyDatabase/GenerateClassbyDatabase/File/";
        Main(filePath);
        Console.WriteLine("GenerateClassbyDatabase done!");
        Console.WriteLine();
    }
}
static List<(int, string)> GetDataBaseList(string connectionString)
{
    string dataBaseQuery = "SELECT dbid,name FROM master.dbo.sysdatabases order by dbid";
    var results = new List<(int, string)>();
    using (var conn = new SqlConnection(connectionString))
    {
        results = conn.Query<(int, string)>(dataBaseQuery).ToList();
        Console.WriteLine($"\n資料庫清單：\n");
        foreach (var item in results)
        {
            Console.WriteLine($"{item.Item1}：{item.Item2}");
        }
    }
    return results;
}
static string GetConnectionString(string dataBase = "")
{
    return $"Server=you host port;Database={(!string.IsNullOrEmpty(dataBase) ? dataBase : string.Empty)};User ID=xxxx;Password=xxxx";
}
static void Main(string filePath)
{
    string connectionString = GetConnectionString();
    //string dataBase = Console.ReadLine(); 
    var results = GetDataBaseList(connectionString);
    string dataBase = CheckDataBase(results);
    var tableList = DataBaseTableListQuery(dataBase);
    Console.Write($"\n請輸入查詢資料表號碼，若有多個請使用逗號串連：");
    string tableNo = Console.ReadLine();
    List<string> tableNames = new List<string>();
    try
    {
        if (!string.IsNullOrEmpty(tableNo))
        {
            if (tableNo.Contains(","))
            {
                var tableIndex = tableNo.Replace(" ", "").Split(",").Select(int.Parse);
                var tableNameArray = tableIndex.Select(i => tableList[i].Item2).ToArray();
                tableNames = tableList.Where(x => tableNameArray.Contains(x.Item2)).Select(x => x.Item2).ToList();
            }
            else
            {
                tableNames.Add(tableList.Select(x => x.Item2).ToArray()[int.Parse(tableNo)]);
            }
        }
        if (tableNames.Any())
        {
            connectionString = GetConnectionString(dataBase);
            //取得資料表結構清單 
            var tableSchemas = GetTableChema(connectionString, tableNames);
            Console.WriteLine($"\n是否產生.cs檔案? 確定請輸入：1");
            string check = Console.ReadLine();
            if (check == "1")
            {
                //產生.cs檔案 
                //var fileSource = ConvertToClassFileSource(tableSchema); 
                //GenerateClass(tableSchema.FirstOrDefault()?.TABLE_NAME, fileSource, filePath); 
                foreach (var item in tableSchemas)
                {
                    GenerateClass(item.Key, item.Value, filePath);
                }

            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine("索引超出陣列範圍！");
    }
    Console.WriteLine("\n程序結束");
    Console.ReadKey();
}
static List<(string propertyName, string propertyType, string description)> ConvertToClassFileSource(List<INFO_SCHEMA_COLUMNS> tableChemas)
{
    var properties = new List<(string propertyName, string propertyType, string description)>();
    //產生.cs檔案 
    int maxName = tableChemas.Max(x => x.COLUMN_NAME?.Length ?? 0);
    int maxType = tableChemas.Max(x => x.DATA_TYPE?.Length ?? 0);
    int maxLength = tableChemas.Max(x => x.CHARACTER_MAXIMUM_LENGTH?.Length ?? 0);
    foreach (var item in tableChemas)
    {
        string name = item.COLUMN_NAME;
        string type = ConvertSqlServerFormatToCSharp(item.DATA_TYPE);
        type = type += item.IS_NULLABLE == "YES" ? "?" : string.Empty;
        string description = item.Description;
        properties.Add(new(name, type, description));
    }
    return properties;
}
//查詢資料庫，資料表 
static List<(string, string)> DataBaseTableListQuery(string dataBase)
{
    List<(string, string)> results = new List<(string, string)>();
    string connectionString = GetConnectionString(dataBase);
    string tableListQuery = "select TABLE_CATALOG,Table_name from INFORMATION_SCHEMA.TABLES order by Table_name";
    using (var conn = new SqlConnection(connectionString))
    {
        results = conn.Query<(string, string)>(tableListQuery).ToList();
        Console.WriteLine($"\n資料庫：{results.FirstOrDefault().Item1}\n資料表：");
        for (int i = 0; i < results.Count(); i++)
        {
            Console.WriteLine($"{i}：{results[i].Item2}");
        }
    }
    return results;
}
// Class 產生   
static void GenerateClass(string className, List<(string propertyName, string propertyType, string description)> properties, string filePath)
{
    string fileName = className + ".cs";
    string namespaceName = "ProjectName.Modeles";
    string sourcePath = Path.Combine(filePath, fileName);
    using (StreamWriter streamWriter = new StreamWriter(sourcePath))
    {
        // 加入檔頭     
        streamWriter.WriteLine("using System;");
        streamWriter.WriteLine("using System.ComponentModel;");
        streamWriter.WriteLine();
        // 加入命名空間     
        streamWriter.WriteLine($"namespace {namespaceName}");
        streamWriter.WriteLine("{");
        streamWriter.WriteLine($"\tpublic class {className}");
        streamWriter.WriteLine("\t{");
        // 加入屬性     
        foreach (var property in properties)
        {
            streamWriter.WriteLine($"\t\t/// <summary>");
            streamWriter.WriteLine($"\t\t/// {property.description}");
            streamWriter.WriteLine($"\t\t/// </summary>");
            streamWriter.WriteLine($"\t\t[Description(\"{property.propertyName}\")]");
            streamWriter.WriteLine($"\t\tpublic {property.propertyType} {property.propertyName} {{ get; set; }}");
            streamWriter.WriteLine();
        }
        // 加入檔尾     
        streamWriter.WriteLine("\t}");
        streamWriter.WriteLine("}");
    }
}
static string[] SqlServerTypes()
{
    string[] SqlServerTypes = { "bigint", "binary", "bit", "char", "date", "datetime", "datetime2", "datetimeoffset", "decimal", "filestream", "float", "geography", "geometry", "hierarchyid", "image", "int", "money", "nchar", "ntext", "numeric", "nvarchar", "real", "rowversion", "smalldatetime", "smallint", "smallmoney", "sql_variant", "text", "time", "timestamp", "tinyint", "uniqueidentifier", "varbinary", "varchar", "xml" };
    return SqlServerTypes;
}
static string[] CSharpTypes()
{
    string[] CSharpTypes = { "long", "byte[]", "bool", "char", "DateTime", "DateTime", "DateTime", "DateTimeOffset", "decimal", "byte[]", "double", "Microsoft.SqlServer.Types.SqlGeography", "Microsoft.SqlServer.Types.SqlGeometry", "Microsoft.SqlServer.Types.SqlHierarchyId", "byte[]", "int", "decimal", "string", "string", "decimal", "string", "Single", "byte[]", "DateTime", "short", "decimal", "object", "string", "TimeSpan", "byte[]", "byte", "Guid", "byte[]", "string", "string" };
    return CSharpTypes;
}
static string ConvertSqlServerFormatToCSharp(string typeName)
{
    var index = Array.IndexOf(SqlServerTypes(), typeName);
    return index > -1
        ? CSharpTypes()[index]
        : "object";
}
string ConvertCSharpFormatToSqlServer(string typeName)
{
    var index = Array.IndexOf(CSharpTypes(), typeName);
    return index > -1 ? SqlServerTypes()[index] : null;
}
static Dictionary<string, List<(string propertyName, string propertyType, string description)>> GetTableChema(string connectionString, List<string> tableNames)
{
    string querySchema = GetTableSchemaString();
    var dictionary = new Dictionary<string, List<(string propertyName, string propertyType, string description)>>();
    using (var conn = new SqlConnection(connectionString))
    {
        foreach (var tableName in tableNames)
        {
            List<INFO_SCHEMA_COLUMNS> results = new List<INFO_SCHEMA_COLUMNS>();
            results = conn.Query<INFO_SCHEMA_COLUMNS>(querySchema, new { table_name = tableName }).ToList();
            int maxName = results.Max(x => x.COLUMN_NAME?.Length ?? 0);
            int maxKey = results.Max(x => x.COLUMN_KEY?.Length ?? 0);
            int maxType = results.Max(x => x.DATA_TYPE?.Length ?? 0);
            int maxLength = results.Max(x => x.CHARACTER_MAXIMUM_LENGTH?.Length ?? 0);
            int maxDefalut = results.Max(x => x.COLUMN_DEFAULT?.Length ?? 0);
            int maxIsNull = results.Max(x => x.IS_NULLABLE?.Length ?? 0);
            Console.WriteLine($"\n資料庫：{results.FirstOrDefault()?.TABLE_CATALOG} - 資料表：{results.FirstOrDefault()?.TABLE_NAME}\n");
            foreach (var item in results)
            {
                Console.WriteLine($"欄位：{item.COLUMN_NAME.PadRight(maxName)} - 是否為Key：{item.COLUMN_KEY.PadRight(maxKey)} - 型態：{item.DATA_TYPE.PadRight(maxType)} - 長度：{item.CHARACTER_MAXIMUM_LENGTH?.PadRight(maxLength) ?? string.Empty.PadRight(maxLength)} - 預設值：{item.COLUMN_DEFAULT?.PadRight(maxDefalut) ?? string.Empty.PadRight(maxDefalut)} - 允許Null：{item.IS_NULLABLE.PadRight(maxIsNull)} - 描述：{item.Description}\n");
            }
            //產生.cs檔案 
            var fileSource = ConvertToClassFileSource(results);
            dictionary.Add(results.FirstOrDefault().TABLE_NAME, fileSource);
            Console.WriteLine("------------------------------------------------------");
        }
    }
    return dictionary;
}
//資料表結構 
static string GetTableSchemaString()
{
    return @"SELECT 
            a.TABLE_NAME               , 
            b.COLUMN_NAME              , 
            ISNULL((SELECT CASE WHEN COLUMN_NAME IS NOT NULL AND COLUMN_NAME <> '' THEN 'Yes' ELSE 'No' END　FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE WHERE TABLE_NAME = a.TABLE_NAME and COLUMN_NAME = b.COLUMN_NAME),'NO') as COLUMN_KEY, 
            b.DATA_TYPE                , 
            b.CHARACTER_MAXIMUM_LENGTH , 
            b.COLUMN_DEFAULT           , 
            b.IS_NULLABLE              , 
            ( 
                SELECT 
                    value 
                FROM 
                    fn_listextendedproperty (NULL, 'schema', 'dbo', 'table',  
                                                a.TABLE_NAME, 'column', default) 
                WHERE 
                    name='MS_Description'  
                    and objtype='COLUMN'  
                    and objname Collate Chinese_Taiwan_Stroke_CI_AS=b.COLUMN_NAME 
            ) as Description 
            FROM 
                INFORMATION_SCHEMA.TABLES  a 
                LEFT JOIN INFORMATION_SCHEMA.COLUMNS b ON (a.TABLE_NAME=b.TABLE_NAME) 
            WHERE a.TABLE_NAME= @table_name ORDER BY a.TABLE_NAME, b.ORDINAL_POSITION";
}
static string CheckDataBase(List<(int, string)> results)
{
    Console.Write($"\n請輸入目標資料庫編號：");
    string no = Console.ReadLine();
    string dataBase = string.Empty;
    if (int.TryParse(no, out var r))
    {
        dataBase = results.FirstOrDefault(x => x.Item1 == r).Item2 ?? string.Empty;        
    }

    if (string.IsNullOrEmpty(dataBase))
    {
        Console.WriteLine("無此資料庫請重新輸入\n");
        CheckDataBase(results);
    }

    return dataBase;
}
class INFO_SCHEMA_COLUMNS
{
    public string TABLE_CATALOG { get; set; }
    public string TABLE_NAME { get; set; }
    public string COLUMN_KEY { get; set; }
    public string COLUMN_NAME { get; set; }
    public string DATA_TYPE { get; set; }
    public string CHARACTER_MAXIMUM_LENGTH { get; set; }
    public string COLUMN_DEFAULT { get; set; }
    public string IS_NULLABLE { get; set; }
    public string Description { get; set; }
}