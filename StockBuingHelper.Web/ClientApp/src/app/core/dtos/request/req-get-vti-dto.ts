export interface ReqGetVtiDto
{
    queryType: string;
    specificStockId?: string;
    vtiIndex: number[];
    queryEtfs: boolean;
}
  