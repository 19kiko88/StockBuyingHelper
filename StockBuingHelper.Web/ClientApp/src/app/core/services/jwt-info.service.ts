import { JwtService } from '../http/jwt.service';
import { Injectable } from '@angular/core';
import { BaseService } from '../http/base.service';
import { Subject, lastValueFrom } from 'rxjs';
import { UserInfo } from '../models/user-info';

@Injectable({
  providedIn: 'root'
})
export class JwtInfoService  extends BaseService
{

  private _jwt?: string;
  private emitJwtValid = new Subject<boolean>();
  jwtValid$ = this.emitJwtValid.asObservable();

  constructor(
    private _httpJwtService: JwtService
  ) 
  { 
    super();    
  }
  
  get jwt(): string | undefined
  {
    this._jwt = localStorage.getItem('jwt') ?? '';
    return this._jwt;
  }

  set jwt(value: string)
  {
    localStorage.setItem('jwt', value);
    this._jwt = value;
  }

  get jwtExpired():boolean
  {  
    if (this.jwt)
    {
      //window.atob => base64解碼
      const payload = JSON.parse(window.atob(this.jwt.split('.')[1]));
      const exp = new Date(Number(payload.exp) * 1000)
      if (new Date() > exp)
      {      
        return true;
      }
    }

    return false;
  }

  get jwtPayload(): UserInfo|undefined
  {
    if (this.jwt)
    {
      let payload = JSON.parse(window.atob(this.jwt.split('.')[1]));        
      let userInfo: UserInfo = {account: payload[ClaimTypes.Account], name: payload[ClaimTypes.Name], email: payload[ClaimTypes.Email], role: payload[ClaimTypes.Role] };    
      return userInfo;
    }
    else 
    {
      return undefined;
    }
  }

  setJwtValid(valid: boolean)
  {
    this.emitJwtValid.next(valid);
  }

  async jwtSignatureVerify(): Promise<boolean>
  {
    let res = false
    if (this._jwt)
    {
      res = await lastValueFrom(this._httpJwtService.JwtSignatureVerify(this._jwt));
    }    
    return res;
  }

}

enum ClaimTypes {
  Account = 'Account',
  Name = 'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name',
  Email = 'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress',
  Role = 'http://schemas.microsoft.com/ws/2008/06/identity/claims/role'
}
