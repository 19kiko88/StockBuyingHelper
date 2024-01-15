using StockBuyingHelper.Service.Models;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StockBuyingHelper.Service.Interfaces;
using StockBuyingHelper.Models;
using Microsoft.Extensions.Options;

namespace StockBuyingHelper.Service.Implements
{
    public class VolumeService: IVolumeService
    {
        private readonly AppSettings.ConnectionStrings _conn;

        public VolumeService(IOptions<AppSettings.ConnectionStrings> conn) 
        {
            _conn = conn.Value;
        }

        public async Task<List<StockVolumeInfoModel>> GetDbVolumeDetail()
        {
            var res = new List<StockVolumeInfoModel>() { };            
            string strSQL = @"select *from Volume_Detail with(nolock)";

            using (var cn = new SqlConnection(_conn.SBHConnection))
            {
                cn.Open();
                using (SqlCommand cmd = new SqlCommand(strSQL, cn))
                {
                    //1.回傳DataSet or DataTable 
                    using (SqlDataAdapter adpter = new SqlDataAdapter(cmd))
                    {
                        var ds = new DataSet();
                        adpter.Fill(ds);
                        var dt = ds.Tables[0];

                        var currentId = "";
                        var volumeData = new List<VolumeData>();
                        var cnt = 0;
                        foreach (DataRow dr in dt.Rows)
                        {
                            cnt++;
                            if (string.IsNullOrEmpty(currentId))
                            {
                                currentId = dr["Stock_Id"].ToString();
                            }

                            if (currentId != dr["Stock_Id"].ToString())
                            {
                                res.Add(new StockVolumeInfoModel()
                                {
                                    StockId = currentId,
                                    VolumeInfo = volumeData
                                });

                                currentId = dr["Stock_Id"].ToString();
                                volumeData = new List<VolumeData>();
                            }

                            volumeData.Add(new VolumeData()
                            {
                                txDate = DateOnly.FromDateTime(Convert.ToDateTime(dr["Tx_Date"].ToString())),
                                foreignDiffVolK = (int)dr["Foreign_Diff_VolK"],
                                dealerDiffVolK = (int)dr["Dealer_Diff_VolK"],
                                investmentTrustDiffVolK = (int)dr["InvestmentTrust_Diff_VolK"],
                                volumeK = (int)dr["VolumeK"],
                            });

                            if (cnt == dt.Rows.Count)
                            {
                                res.Add(new StockVolumeInfoModel()
                                {
                                    StockId = currentId,
                                    VolumeInfo = volumeData
                                });
                            }
                        }
                    }
                }
            }

            return res;
        }

        /// <summary>
        /// 
        /// Ref：https://stackoverflow.com/questions/13741879/how-to-add-sqlparameters-in-a-loop
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public void InsertVolumeDetail(List<StockVolumeInfoModel> data)
        {
            //string strSQL = @"insert into Volume_Detail (Stock_Id, Tx_Date, Foreign_Sell_VolK, Dealer_Sell_VolK, InvestmentTrust_Diff_VolK, VolumeK)  values  (@Stock_Id, @Tx_Date, @Foreign_Sell_VolK, @Dealer_Sell_VolK, @InvestmentTrust_Diff_VolK, @VolumeK);";
            //string sqlCmd = string.Empty;

            using (var cn = new SqlConnection(_conn.SBHConnection))
            {
                //var ts = cn.BeginTransaction(IsolationLevel.ReadUncommitted);

                try
                {
                    cn.Open();

                    var dt = new DataTable();
                    dt.Columns.Add("Stock_Id", typeof(int));
                    dt.Columns.Add("Tx_Date", typeof(DateTime));
                    dt.Columns.Add("Foreign_Diff_VolK", typeof(int));
                    dt.Columns.Add("Dealer_Diff_VolK", typeof(int));
                    dt.Columns.Add("InvestmentTrust_Diff_VolK", typeof(int));
                    dt.Columns.Add("VolumeK", typeof(int));
                    foreach (var volume in data)
                    {
                        foreach (var volumeDetail in volume.VolumeInfo)
                        {
                            DataRow dr = dt.NewRow();
                            dr["Stock_Id"] = volume.StockId;
                            dr["Tx_Date"] = new DateTime(volumeDetail.txDate.Year, volumeDetail.txDate.Month, volumeDetail.txDate.Day);
                            dr["Foreign_Diff_VolK"] = volumeDetail.foreignDiffVolK;
                            dr["Dealer_Diff_VolK"] = volumeDetail.dealerDiffVolK;
                            dr["InvestmentTrust_Diff_VolK"] = volumeDetail.investmentTrustDiffVolK;
                            dr["VolumeK"] = volumeDetail.volumeK;
                            dt.Rows.Add(dr);
                        }
                    }


                    using (var sqlBC = new SqlBulkCopy(cn))
                    {
                        //設定一個批次量寫入多少筆資料
                        sqlBC.BatchSize = 1000;
                        //設定逾時的秒數
                        sqlBC.BulkCopyTimeout = 60;

                        //設定 NotifyAfter 屬性，以便在每複製 10000 個資料列至資料表後，呼叫事件處理常式。
                        sqlBC.NotifyAfter = dt.Rows.Count;
                        //sqlBC.SqlRowsCopied += new SqlRowsCopiedEventHandler(CallSqlRowsCopied);

                        //設定要寫入的資料庫
                        sqlBC.DestinationTableName = "dbo.Volume_Detail";

                        //對應資料行 (來源欄位,目的地欄位)
                        sqlBC.ColumnMappings.Add("Stock_Id", "Stock_Id");
                        sqlBC.ColumnMappings.Add("Tx_Date", "Tx_Date");
                        sqlBC.ColumnMappings.Add("Foreign_Diff_VolK", "Foreign_Diff_VolK");
                        sqlBC.ColumnMappings.Add("Dealer_Diff_VolK", "Dealer_Diff_VolK");
                        sqlBC.ColumnMappings.Add("InvestmentTrust_Diff_VolK", "InvestmentTrust_Diff_VolK");
                        sqlBC.ColumnMappings.Add("VolumeK", "VolumeK");

                        //開始寫入
                        sqlBC.WriteToServer(dt);

                        //完成交易
                        //ts.Commit();
                    }
                }
                catch (Exception ex)
                {
                    //ts.Rollback();
                }
            }

            //using (var cn = new SqlConnection(_conn.SBHConnection))
            //{
            //    cn.Open();
            //    var tran = cn.BeginTransaction(IsolationLevel.ReadUncommitted);
            //    var cnt = data.Select(c => c.VolumeInfo).Count();
            //    for (int idx = 0; idx < cnt; idx++)
            //    {
            //        sqlCmd += $"insert into Volume_Detail (Stock_Id, Tx_Date, Foreign_Diff_VolK, Dealer_Sell_VolK, InvestmentTrust_Diff_VolK, VolumeK)  values  (@Stock_Id_{idx}, @Tx_Date_{idx}, @Foreign_Sell_VolK_{idx}, @Dealer_Sell_VolK_{idx}, @InvestmentTrust_Diff_VolK_{idx}, @VolumeK_{idx});";
            //    }

            //    try
            //    {
            //        using (var cmd = new SqlCommand(sqlCmd, cn, tran))
            //        {
            //            var idx = 0;
            //            foreach (var volume in data)
            //            {
            //                foreach (var volumeDetail in volume.VolumeInfo)
            //                {
            //                    cmd.Parameters.Add($"@Stock_Id_{idx}", SqlDbType.VarChar).Value = volume.StockId;
            //                    cmd.Parameters.Add($"@Tx_Date_{idx}", SqlDbType.DateTime).Value = new DateTime(volumeDetail.txDate.Year, volumeDetail.txDate.Month, volumeDetail.txDate.Day);
            //                    cmd.Parameters.Add($"@Foreign_Diff_VolK_{idx}", SqlDbType.Int).Value = volumeDetail.foreignDiffVolK;
            //                    cmd.Parameters.Add($"@Dealer_Diff_VolK_{idx}", SqlDbType.Int).Value = volumeDetail.dealerDiffVolK;
            //                    cmd.Parameters.Add($"@InvestmentTrust_Diff_VolK_{idx}", SqlDbType.Int).Value = volumeDetail.investmentTrustDiffVolK;
            //                    cmd.Parameters.Add($"@VolumeK_{idx}", SqlDbType.Int).Value = volumeDetail.volumeK;
            //                    idx++;
            //                }
            //            }
            //            //for (int i = 0; i < data.Count; i++)
            //            //{
            //            //    var idx = 0;
            //            //    foreach (var item in data[i].VolumeInfo)
            //            //    {                                
            //            //        cmd.Parameters.Add($"@Stock_Id_{idx}", SqlDbType.VarChar).Value = data[i].StockId;
            //            //        cmd.Parameters.Add($"@Tx_Date_{idx}", SqlDbType.DateTime).Value = new DateTime(item.txDate.Year, item.txDate.Month, item.txDate.Day);
            //            //        cmd.Parameters.Add($"@Foreign_Diff_VolK_{idx}", SqlDbType.Int).Value = item.foreignDiffVolK;
            //            //        cmd.Parameters.Add($"@Dealer_Diff_VolK_{idx}", SqlDbType.Int).Value = item.dealerDiffVolK;
            //            //        cmd.Parameters.Add($"@InvestmentTrust_Diff_VolK_{idx}", SqlDbType.Int).Value = item.investmentTrustDiffVolK;
            //            //        cmd.Parameters.Add($"@VolumeK_{idx}", SqlDbType.Int).Value = item.volumeK;
            //            //        idx++;
            //            //    }
            //            //}
            //            cmd.ExecuteNonQuery();
            //        }

            //        tran.Commit();
            //    }
            //    catch (Exception ex)
            //    {
            //        tran.Rollback();
            //    }



            //    //foreach (var item in data)
            //    //{
            //    //    try
            //    //    {
            //    //        using (var cmd = new SqlCommand(strSQL, cn))
            //    //        {            
                            

            //    //            cmd.Parameters.Add("@Stock_Id", SqlDbType.VarChar).Value = stockId;
            //    //            cmd.Parameters.Add("@Tx_Date", SqlDbType.DateTime).Value = new DateTime(item.txDate.Year, item.txDate.Month, item.txDate.Day);
            //    //            cmd.Parameters.Add("@Foreign_Sell_VolK", SqlDbType.Int).Value = item.foreignDiffVolK;
            //    //            cmd.Parameters.Add("@Dealer_Sell_VolK", SqlDbType.Int).Value = item.dealerDiffVolK;
            //    //            cmd.Parameters.Add("@InvestmentTrust_Diff_VolK", SqlDbType.Int).Value = item.investmentTrustDiffVolK;
            //    //            cmd.Parameters.Add("@VolumeK", SqlDbType.Int).Value = item.volumeK;
            //    //            cmd.ExecuteNonQuery();
            //    //        }
            //    //    }
            //    //    catch (Exception ex)
            //    //    {
            //    //        cn.Close();
            //    //    }
            //    //}
            //}
        }
    }
}
