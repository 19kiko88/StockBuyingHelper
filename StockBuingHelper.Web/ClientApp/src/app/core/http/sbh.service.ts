import { Injectable } from '@angular/core';
import { BaseService } from './base.service';
import { environment } from 'src/environments/environment';
import { IResultDto } from '../../core/dtos/response/result-dto';
import { HttpClient } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { ReqGetVtiDto } from '../../core/dtos/request/req-get-vti-dto';
import { ResGetVtiDto } from '../../core/dtos/response/res-get-vti-dto';

@Injectable({
  providedIn: 'root'
})
export class SbhService extends BaseService{

  constructor(
    private _httpClient: HttpClient
  ) 
  {
    super();
   }

  GetVtiData(data: ReqGetVtiDto):Observable<IResultDto<ResGetVtiDto[]>>
  {
    const url = `${environment.apiBaseUrl}/Stock/GetVtiData`;
    const options = this.generatePostOptions();    

    return this._httpClient.post<IResultDto<ResGetVtiDto[]>>(url, data, options);
    //.pipe(map( res => this.processResult(res)));
  }
}
