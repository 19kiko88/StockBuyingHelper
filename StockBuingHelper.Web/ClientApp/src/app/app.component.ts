import { Component, OnInit } from '@angular/core';
import { JwtInfoService } from './core/services/jwt-info.service';
import { UserInfo } from './core/models/user-info';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.css']
})

export class AppComponent implements OnInit {
  title = '買股小幫手';

  jwtValidate: boolean = false;
  isLoader: boolean = false;
  loadingMsg: string|undefined = '';
  jwtvalid: boolean = false;
  userInfo?: UserInfo;

  constructor(
    private _jwtService: JwtInfoService,
  ) 
  {}


  ngOnInit(): void 
  {
    // if(this._jwtService.jwt && !this._jwtService.jwtExpired)
    // {
    //   this._jwtService.jwtSignatureVerify().then(res => {
    //     alert(res);
    //   })
    // }

    this._jwtService.jwtValid$.subscribe(res => {
      this.jwtvalid = res;

      if (res && this._jwtService.jwt) 
      {
        let payload = JSON.parse(window.atob(this._jwtService.jwt.split('.')[1]));
        
        this.userInfo = {
          account: payload.Account,
          name: payload.Name,
          email: payload.Email,
          role: payload.Role
        };        
      }
      
    })
  }
  
}
