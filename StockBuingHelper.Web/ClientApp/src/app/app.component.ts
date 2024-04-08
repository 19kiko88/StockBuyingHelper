import { Component, OnInit } from '@angular/core';
import { JwtInfoService } from './core/services/jwt-info.service';
import { UserInfo } from './core/models/user-info';
import { NavigationStart, Router } from '@angular/router';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.css']
})

export class AppComponent implements OnInit {
  title = '買股小幫手';
  isLoader: boolean = false;
  loadingMsg: string|undefined = '';
  userInfo?: UserInfo;
  login: boolean = false;

  constructor(  
    private _route: Router,  
    private _jwtService: JwtInfoService,
  ) 
  {
    _route.events.forEach((event) => 
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
          if(_jwtService.jwt)
          {
            let payload = JSON.parse(window.atob(_jwtService.jwt.split('.')[1]));        
            this.userInfo = {account: payload.Account, name: payload.Name, email: payload.Email, role: payload.Role };    
          }
        }
      }
    });
  }


  ngOnInit(): void 
  {
    // if(this._jwtService.jwt && !this._jwtService.jwtExpired)
    // {
    //   this._jwtService.jwtSignatureVerify().then(res => {
    //     alert(res);
    //   })
    // }

    // this._jwtService.jwtValid$.subscribe(res => {
    //   this.jwtvalid = res;

    //   if (res && this._jwtService.jwt) 
    //   {
    //     let payload = JSON.parse(window.atob(this._jwtService.jwt.split('.')[1]));
        
    //     this.userInfo = {
    //       account: payload.Account,
    //       name: payload.Name,
    //       email: payload.Email,
    //       role: payload.Role
    //     };        
    //   }
      
    // })
  }
  
}
