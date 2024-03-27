import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup } from '@angular/forms';
import { LoginDto } from 'src/app/core/dtos/request/login-dto';
import { LoginService } from 'src/app/core/http/login.service';

@Component({
  selector: 'app-login',
  //standalone: true,
  //imports: [],
  templateUrl: './login.component.html',
  styleUrl: './login.component.css'
})

export class LoginComponent implements OnInit 
{
  form!:FormGroup;
  errorMessage: string = '';  
  account?: string = '';
  password?: string = '';

  constructor(
    private _loginService: LoginService,
    private _formBuilder: FormBuilder,
  ){

  }

  ngOnInit(): void {
    this.form = this._formBuilder.group({
      specificStockId: [''],
      vtiRanges: [],
      etfDisplay : false
    })
  }

  login()
  {
    debugger
    let data:LoginDto = {Account: this.account, Password: this.password}
    this._loginService.JwtLogin(data).subscribe({
      next: c => {
        alert('qq~');
      },
      error: () => {

      }
    })
  }

}
