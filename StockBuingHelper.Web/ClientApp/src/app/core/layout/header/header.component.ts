import { Router } from '@angular/router';
import { UserInfo } from '../../models/user-info';
import { AfterViewInit, Component, Input, OnInit } from '@angular/core';
import { JwtInfoService } from '../../services/jwt-info.service';
import { environment } from 'src/environments/environment';

@Component({
  selector: 'app-header',
  templateUrl: './header.component.html',
  styleUrls: ['./header.component.css']
})
export class HeaderComponent implements OnInit, AfterViewInit
{

  envName: string = '';
  userAccount: string = '';
  @Input() inputUserInfo : UserInfo | undefined

  constructor(
    private _jwtInfoService: JwtInfoService,
    private _router: Router
  ) { }

  ngOnInit(): void 
  {
    this.envName = `${environment.envName}`;
    this.userAccount = this.inputUserInfo?.account ?? '';
  }

  ngAfterViewInit()
  {
    //註冊選單漢堡click事件
    utilObj.sidebarToggle();
  }
  
  logOut()
  {
    window.alert('bye bye~');
    this._jwtInfoService.removeJwt();
    this._router.navigate(['/login']);
  }
}
