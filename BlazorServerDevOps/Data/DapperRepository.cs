using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Dapper;
using Dapper.Contrib.Extensions;
using System.Data;

public class DapperRepository<TEntity> : IRepository<TEntity> where TEntity : class
{
    private string _sqlConnectionString;
    private string entityName;
    private Type entityType;
    private string primaryKeyName;
    private string primaryKeyType;
    private bool PKNotIdentity = false;

    public DapperRepository(string sqlConnectionString)
    {
        _sqlConnectionString = sqlConnectionString;
        entityType = typeof(TEntity);
        entityName = entityType.Name;

        var props = entityType.GetProperties().Where(
            prop => Attribute.IsDefined(prop,
            typeof(KeyAttribute)));
        if (props.Count() > 0)
        {
            primaryKeyName = props.First().Name;
            primaryKeyType = props.First().PropertyType.Name;
        }
        else
        {
            // Default
            primaryKeyName = "Id";
            primaryKeyType = "Int32";
        }

        // look for [ExplicitKey]
        props = entityType.GetProperties().Where(
            prop => Attribute.IsDefined(prop,
            typeof(ExplicitKeyAttribute)));
        if (props.Count() > 0)
        {
            PKNotIdentity = true;
            primaryKeyName = props.First().Name;
            primaryKeyType = props.First().PropertyType.Name;
        }
    }

    public async Task<bool> DeleteAsync(TEntity entityToDelete)
    {
        using (IDbConnection db = new SqlConnection(_sqlConnectionString))
        {

            try
            {
                await db.DeleteAsync<TEntity>(entityToDelete);
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

    }

    public async Task<IEnumerable<TEntity>> GetAsync(string Query)
    {
        using (IDbConnection db = new SqlConnection(_sqlConnectionString))
        {
            try
            {
                return await db.QueryAsync<TEntity>(Query);
            }
            catch (Exception ex)
            {
                return (IEnumerable<TEntity>)new List<TEntity>();
            }
        }
    }

    public async Task<IEnumerable<TEntity>> GetAsync(Expression<Func<TEntity, bool>>
        filter = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderBy = null,
        string includeProperties = "")
    {
        throw new NotImplementedException();
    }

    public async Task<IEnumerable<TEntity>> GetAllAsync()
    {
        using (IDbConnection db = new SqlConnection(_sqlConnectionString))
        {
            db.Open();
            return await db.GetAllAsync<TEntity>();
        }
    }

    public async Task<TEntity> InsertAsync(TEntity entity)
    {
        using (IDbConnection db = new SqlConnection(_sqlConnectionString))
        {
            db.Open();
            await db.ExecuteAsync("begin transaction");
            try
            {
                // Get the primary key property
                var prop = entityType.GetProperty(primaryKeyName);

                // int key?
                if (primaryKeyType == "Int32")
                {
                    // not an identity?
                    if (PKNotIdentity == true)
                    {
                        // get the highest value
                        var sql = $"select max({primaryKeyName}) from {entityName}";
                        // and add 1 to it
                        var Id = Convert.ToInt32(db.ExecuteScalar(sql)) + 1;
                        // update the entity
                        prop.SetValue(entity, Id);
                        // do the insert
                        db.Insert<TEntity>(entity);
                    }
                    else
                    {
                        // key will be created by the database
                        var Id = (int)db.Insert<TEntity>(entity);
                        // set the value
                        prop.SetValue(entity, Id);
                    }
                }
                else if (primaryKeyType == "String")
                {
                    // string primary key. Use my helper
                    string sql = DapperSqlHelper.GetDapperInsertStatement(entity, entityName);
                    await db.ExecuteAsync(sql, entity);
                }
                // if we got here, we're good!
                await db.ExecuteAsync("commit transaction");
                return entity;
            }
            catch (Exception ex)
            {
                var msg = ex.Message;
                await db.ExecuteAsync("rollback transaction");
                return null;
            }
        }
    }

    public async Task<TEntity> UpdateAsync(TEntity entity)
    {
        using (IDbConnection db = new SqlConnection(_sqlConnectionString))
        {
            db.Open();
            try
            {
                //string sql = DapperSqlHelper.GetDapperUpdateStatement(entity, entityName, primaryKeyName);
                //await db.ExecuteAsync(sql, entity);
                await db.UpdateAsync<TEntity>(entity);
                return entity;
            }
            catch (Exception ex)
            {
                return null;
            }
        }
    }
}
