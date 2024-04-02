import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { LoginDto } from 'src/app/core/dtos/request/login-dto';
import { LoginService } from 'src/app/core/http/login.service';
import { JwtInfoService } from 'src/app/core/services/jwt-info.service';
import * as forge from 'node-forge';

@Component({
  selector: 'app-login',
  //standalone: true,
  //imports: [],
  templateUrl: './login.component.html',
  styleUrl: './login.component.css'
})

export class LoginComponent implements OnInit 
{
  errorMessage: string = '';  
  account?: string = '';
  password: string = '';
  publicKey: string = `-----BEGIN PUBLIC KEY-----
  MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEArc/vhkP0RV7wQE3LbJpS
  m4ony6aE+CcFu6ky3r/IIplOh86yGkk+EUPrufQZ4K0naR6xgnL1Puv6WeiCzZj/
  0JQeaeZUGweO2mx9TazXU4VqT95F+IwUJrhTVmJu/JwfNLjQ+cgo8WadZ2DB2jGs
  SN1d3oGoRXiINZhsUsVJw6tkAuh3IIeAkVXeWaVJE0I0est+xX+g4sgz4UC23jxB
  NZJJmXiBwOvAQ3Mg/DWBdBmuWweQCgr9Tc//KLCwE+xY1mZYu0DXR/JUecmbbrC4
  BGZm0rogSSP8qd/5xVk7nVovbhT8iz/e4dylXuflA9dYrPrXkg7WEfya7/5If9kw
  SwIDAQAB
  -----END PUBLIC KEY-----`;

  constructor(    
    private _loginService: LoginService,
    private _router: Router,
    private _jwtService: JwtInfoService
  ){

  }

  ngOnInit(): void 
  {
  }

  login()
  {
    var rsa = forge.pki.publicKeyFromPem(this.publicKey);
    var encryptedPassword = window.btoa(rsa.encrypt(this.password));


    let data:LoginDto = {Account: this.account, Password: encryptedPassword}
    this._loginService.JwtLogin(data).subscribe({
      next: res => {
        if (res.message)
        {          
          this.errorMessage = res.message;
          return;
        }
        else
        {//沒有錯誤訊息
          this._jwtService.jwt = res.content;
          this._jwtService.setJwtValid(true);
          this._router.navigate(['/main']);
        }
      },
      error: ex => 
      {
        alert(ex.message);
        return;
      }
    })
  }

}
