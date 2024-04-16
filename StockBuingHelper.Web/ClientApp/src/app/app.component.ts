import { Component } from '@angular/core';
import { JwtInfoService } from './core/services/jwt-info.service';
import { UserInfo } from './core/models/user-info';
import { NavigationStart, Router } from '@angular/router';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.css']
})

export class AppComponent 
{
  title = '買股小幫手';
  isLoader: boolean = false;
  loadingMsg: string|undefined = '';
  userInfo?: UserInfo;
  login: boolean = false;

  constructor
  (  
    private _route: Router,  
    private _jwtService: JwtInfoService,
  ) 
  {
    this._route.events.forEach((event) => 
      {
      if (event instanceof NavigationStart) 
        {
        if (event['url'] == '/login') 
        {
          this.login = false;
        } 
        else 
        {
          this.login = true;  
          this.userInfo = this._jwtService.jwtPayload;
        }
      }
    });
  }

  
}
