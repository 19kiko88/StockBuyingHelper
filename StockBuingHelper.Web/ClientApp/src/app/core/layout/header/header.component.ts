import { UserInfo } from '../../models/user-info';
import { AfterViewInit, Component, Input, OnInit } from '@angular/core';

@Component({
  selector: 'app-header',
  templateUrl: './header.component.html',
  styleUrls: ['./header.component.css']
})
export class HeaderComponent implements OnInit, AfterViewInit
{

  userAccount: string = '';
  @Input() inputUserInfo : UserInfo | undefined

  constructor() { }

  ngOnInit(): void 
  {
    this.userAccount = this.inputUserInfo?.account ?? '';
  }

  ngAfterViewInit()
  {
    //註冊選單漢堡click事件
    utilObj.sidebarToggle();
  }

}
