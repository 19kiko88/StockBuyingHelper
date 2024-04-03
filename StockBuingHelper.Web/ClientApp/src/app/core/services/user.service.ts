import { Injectable } from '@angular/core';
import { UserInfo } from '../models/user-info';
import { Subject } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class UserService {

  private _user?: UserInfo;

  constructor() { }

  get UserInfo(): UserInfo | undefined
  {
    if (this._user)
    {
      return this._user;
    }
    else 
    {
      return undefined;
    }
  }

  set UserInfo(userInfo: UserInfo)
  {
    this._user = userInfo;
  }
}
