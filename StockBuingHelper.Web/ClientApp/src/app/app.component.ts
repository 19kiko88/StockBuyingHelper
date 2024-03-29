import { Component } from '@angular/core';
import { JwtInfoService } from './core/services/jwt-info.service';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.css']
})

export class AppComponent {
  title = '買股小幫手';

  jwtValidate: boolean = false;
  isLoader: boolean = false;
  loadingMsg: string|undefined = '';

  constructor(
    private _jwt: JwtInfoService,
  ) 
  {       
    if(_jwt.jwt && !_jwt.jwtExpired)
    {
      _jwt.jwtSignatureVerifydq().then(res => {
        alert(res);
      })
    }
  }

  
}
