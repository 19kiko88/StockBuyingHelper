import { Injectable } from '@angular/core';
import { LoginDto } from '../dtos/request/login-dto';
import { environment } from 'src/environments/environment';
import { BaseService } from './base.service';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { IResultDto } from '../dtos/response/result-dto';

@Injectable({
  providedIn: 'root'
})
export class AuthService extends BaseService {

  constructor(
    private _httpClient: HttpClient    
  ) {
    super()
   }

  Login(data: LoginDto)
  {
    const url = `${environment.apiBaseUrl}/Auth/Login`;    
    let reqHeaders = new HttpHeaders().set('Content-Type','application/json');    
    let params = JSON.stringify(data);

    return this._httpClient.post<IResultDto<any>>(url, params, {headers:reqHeaders});
    //.pipe(map( res => this.processResult(res)));
  }
}
