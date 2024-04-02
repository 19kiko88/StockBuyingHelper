import { Injectable } from '@angular/core';
import { UserInfo } from '../models/user-info';
import { Subject } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class UserInfoService {

  private _user: UserInfo = { account:'', name:'', email:'', role:''}; 
  private emitLoader = new Subject<UserInfo>();
  loader$ = this.emitLoader.asObservable();

  constructor() { }

  get getUserInfo(): UserInfo
  {
    return this._user;
  }

  set userInfo(jwtPayload: string)
  {
    this._user.account = 'homer_chen';
    this._user.name = '阿好';
    this._user.email = 'test@asus.com';
    this._user.role = '1';

    this.emitLoader.next(this._user);
  }
}
