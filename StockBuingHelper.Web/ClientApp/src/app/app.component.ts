import { Component, OnInit } from '@angular/core';
import { JwtInfoService } from './core/services/jwt-info.service';

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
    })
  }
  
}
