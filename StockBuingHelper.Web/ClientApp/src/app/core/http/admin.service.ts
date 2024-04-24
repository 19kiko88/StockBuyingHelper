import { Injectable } from '@angular/core';
import { environment } from 'src/environments/environment';
import { BaseService } from './base.service';
import { IResultDto } from '../dtos/response/result-dto';
import { Observable } from 'rxjs';
import { HttpClient } from '@angular/common/http';
import { ResGetHistoryDto } from '../dtos/response/res-get-history-dto';

@Injectable({
  providedIn: 'root'
})
export class AdminService extends BaseService {

  constructor(
    private _httpClient: HttpClient
  ) 
  { 
    super();
  }

  
  DeleteVolumeDetail():Observable<IResultDto<number>>
  {
    const url = `${environment.apiBaseUrl}/Admin/DeleteVolumeDetail`;
    const options = this.generatePostOptions();

    return this._httpClient.delete<IResultDto<number>>(url, options);
    //.pipe(map( res => this.processResult(res)));
  }

  RefreshStockList():Observable<IResultDto<number>>
  {
    const url = `${environment.apiBaseUrl}/Admin/RefreshStockList`;
    const options = this.generatePostOptions();

    return this._httpClient.post<IResultDto<number>>(url, options);
  }

  RefreshRevenueInfo():Observable<IResultDto<number>>
  {
    const url = `${environment.apiBaseUrl}/Admin/RefreshRevenueInfo`;
    const options = this.generatePostOptions();

    return this._httpClient.post<IResultDto<number>>(url, options);
  }

  RefreshVolumeInfo():Observable<IResultDto<number>>
  {
    const url = `${environment.apiBaseUrl}/Admin/RefreshVolumeInfo`;
    const options = this.generatePostOptions();

    return this._httpClient.post<IResultDto<number>>(url, options);
  }

  RefreshEpsInfo():Observable<IResultDto<number>>
  {
    const url = `${environment.apiBaseUrl}/Admin/RefreshEpsInfo`;
    const options = this.generatePostOptions();

    return this._httpClient.post<IResultDto<number>>(url, options);
  }

  GetHistory():Observable<IResultDto<ResGetHistoryDto[]>>
  {
    const url = `${environment.apiBaseUrl}/Admin/GetHistory`;
    const options = this.generatePostOptions();

    return this._httpClient.get<IResultDto<ResGetHistoryDto[]>>(url, options);
  }


}
