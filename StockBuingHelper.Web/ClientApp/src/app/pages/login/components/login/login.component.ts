import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { LoginDto } from 'src/app/core/dtos/request/login-dto';
import { LoginService } from 'src/app/core/http/login.service';
import { JwtInfoService } from 'src/app/core/services/jwt-info.service';

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
    let data:LoginDto = {Account: this.account, Password: this.password}
    this._loginService.JwtLogin(data).subscribe({
      next: res => {
        debugger;
        if (res.message)
        {          
          alert(this.errorMessage = res.message);
          return;
        }
        else
        {//沒有錯誤訊息
          this._jwtService.jwt = res.content;
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
