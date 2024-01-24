import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { UserMainComponent } from './components/user-main/user-main.component';



@NgModule({
  declarations: [UserMainComponent],
  imports: [
    CommonModule
  ],
  exports: [UserMainComponent]
})
export class UsersModule { }
