import { UserInfo } from '../../models/user-info';
import { Component, Input, OnInit } from '@angular/core';

@Component({
  selector: 'app-header',
  templateUrl: './header.component.html',
  styleUrls: ['./header.component.css']
})
export class HeaderComponent implements OnInit {

  userAccount: string = '';
  @Input() inputUserInfo : UserInfo | undefined

  constructor() { }

  ngOnInit(): void 
  {
    this.userAccount = this.inputUserInfo?.account ?? '';
  }

}
