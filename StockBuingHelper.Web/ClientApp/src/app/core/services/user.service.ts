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
    return this._user
  }

  set UserInfo(userInfo: UserInfo | undefined)
  {
    this._user = userInfo;
  }
}
