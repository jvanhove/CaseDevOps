using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


public class DapperSqlHelper
{
    public static string GetDapperUpdateStatement(object Entity, string TableName, string PrimaryKeyName)
    {
        string sql = $"update {TableName} set ";
        var EntityType = Entity.GetType();
        var Properties = EntityType.GetProperties();
        foreach (var property in Properties)
        {
            if (Nullable.GetUnderlyingType(property.PropertyType) != null)
            {
                var value = property.GetValue(Entity);
                if (value != null)
                    sql += $"{property.Name} = @{property.Name}, ";
            }
            else if (property.GetGetMethod().IsVirtual == false)
            {
                if (property.Name != PrimaryKeyName)
                {
                    sql += $"{property.Name} = @{property.Name}, ";
                }
            }
        }

        sql = sql.Substring(0, sql.Length - 2);

        sql += $" where {PrimaryKeyName} = @{PrimaryKeyName}";

        return sql;
    }

    public static string GetDapperInsertStatement(object Entity, string TableName)
    {
        string sql = $"insert into {TableName} (";
        var EntityType = Entity.GetType();
        var Properties = EntityType.GetProperties();

        foreach (var property in Properties)
        {
            if (Nullable.GetUnderlyingType(property.PropertyType) != null)
            {
                var value = property.GetValue(Entity);
                if (value != null)
                    sql += $"{property.Name}, ";
            }
            else if (property.GetGetMethod().IsVirtual == false)
            {
                sql += $"{property.Name}, ";
            }
        }

        sql = sql.Substring(0, sql.Length - 2);

        sql += ") values (";

        foreach (var property in Properties)
        {
            if (Nullable.GetUnderlyingType(property.PropertyType) != null)
            {
                var value = property.GetValue(Entity);
                if (value != null)
                    sql += $"@{property.Name}, ";
            }
            else if (property.GetGetMethod().IsVirtual == false)
            {
                sql += $"@{property.Name}, ";
            }
        }

        sql = sql.Substring(0, sql.Length - 2) + ")";

        
        return sql;
    }
}
