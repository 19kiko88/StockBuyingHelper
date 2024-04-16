import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { LoginDto } from 'src/app/core/dtos/request/login-dto';
import { AuthService } from 'src/app/core/http/auth.service';
import { JwtInfoService } from 'src/app/core/services/jwt-info.service';
import * as forge from 'node-forge';
import { LoadingService } from 'src/app/shared/components/loading/loading.service';

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
  publicKey: string =
   `-----BEGIN PUBLIC KEY-----
  MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEArc/vhkP0RV7wQE3LbJpS
  m4ony6aE+CcFu6ky3r/IIplOh86yGkk+EUPrufQZ4K0naR6xgnL1Puv6WeiCzZj/
  0JQeaeZUGweO2mx9TazXU4VqT95F+IwUJrhTVmJu/JwfNLjQ+cgo8WadZ2DB2jGs
  SN1d3oGoRXiINZhsUsVJw6tkAuh3IIeAkVXeWaVJE0I0est+xX+g4sgz4UC23jxB
  NZJJmXiBwOvAQ3Mg/DWBdBmuWweQCgr9Tc//KLCwE+xY1mZYu0DXR/JUecmbbrC4
  BGZm0rogSSP8qd/5xVk7nVovbhT8iz/e4dylXuflA9dYrPrXkg7WEfya7/5If9kw
  SwIDAQAB
  -----END PUBLIC KEY-----`;

  constructor(    
    private _authService: AuthService,
    private _router: Router,
    private _jwtService: JwtInfoService,
    private _loadingService: LoadingService
  ){

  }

  ngOnInit(): void 
  {
  }

  login()
  {
    this._loadingService.setLoading(true, 'Loging...');
    var rsa = forge.pki.publicKeyFromPem(this.publicKey);
    var encryptedPassword = window.btoa(rsa.encrypt(this.password));


    let data:LoginDto = {Account: this.account, Password: encryptedPassword}
    this._authService.Login(data).subscribe({
      next: res => {
        this._loadingService.setLoading(false);
        if (res.message)
        {          
          this.errorMessage = res.message;
          return;
        }
        else
        {//沒有錯誤訊息
          this._jwtService.jwt = res.content;
          this._router.navigate(['/vtiQuery']);
        }
      },
      error: ex => 
      {
        this._loadingService.setLoading(false);
        return;
      }
    })
  }

}
