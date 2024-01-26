import { Injectable } from '@angular/core';
import { environment } from 'src/environments/environment';
import { BaseService } from './base.service';
import { IResultDto } from '../dtos/response/result-dto';
import { map, Observable } from 'rxjs';
import { HttpClient } from '@angular/common/http';

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
    const url = `${environment.apiBaseUrl}/Stock/DeleteVolumeDetail`;
    const options = this.generatePostOptions();    

    return this._httpClient.delete<IResultDto<number>>(url, options);
    //.pipe(map( res => this.processResult(res)));
  }
}
