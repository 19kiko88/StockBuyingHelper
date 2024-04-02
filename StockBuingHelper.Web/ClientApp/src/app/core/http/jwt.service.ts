import { Injectable } from '@angular/core';
import { BaseService } from './base.service';
import { HttpClient } from '@angular/common/http';
import { environment } from 'src/environments/environment';
import { IResultDto } from '../dtos/response/result-dto';
import { Observable, map } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class JwtService extends BaseService {

  constructor(
    private _httpClient: HttpClient
  ) {
    super();
   }

  JwtSignatureVerify(jwt: string): Observable<boolean>
  {
    const url = `${environment.apiBaseUrl}/Login/JwtSignatureVerify?${jwt}`;    
    //let reqHeaders = new HttpHeaders().set('Content-Type','application/json');    
    //let params = JSON.stringify(data);

    return this._httpClient.get<IResultDto<boolean>>(url, this.generateGetOptions()).pipe(map( res => this.processResult(res)));
  }
}
