import { JwtService } from '../http/jwt.service';
import { Injectable } from '@angular/core';
import { environment } from 'src/environments/environment';
import { BaseService } from '../http/base.service';
import { IResultDto } from '../dtos/response/result-dto';
import { Observable, lastValueFrom } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class JwtInfoService  extends BaseService
{

  private _jwt?: string;

  constructor(
    private _httpJwtService: JwtService

  ) 
  { 
    super();
    //this._jwt = localStorage.getItem('jwt') ?? '';    
  }
  
  get jwt(): string | undefined
  {
    return localStorage.getItem('jwt') ?? '';
  }

  set jwt(value: string)
  {
    localStorage.setItem('jwt', value);
    this._jwt = value;
  }

  get jwtExpired():boolean
  {  
    if (this._jwt)
    {
      //window.atob => base64解碼
      const payload = JSON.parse(window.atob(this._jwt.split('.')[1]));
      const exp = new Date(Number(payload.exp) * 1000)
      if (new Date() > exp)
      {      
        return true;
      }
    }

    return false;
  }

  async jwtSignatureVerifydq(): Promise<boolean>
  {
    let res = false
    if (this._jwt)
    {
      res = await lastValueFrom(this._httpJwtService.JwtSignatureVerify(this._jwt));
    }
    return res;
  }

}
