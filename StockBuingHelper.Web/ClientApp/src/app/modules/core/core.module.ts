import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { SideNavComponent } from './layout/side-nav/side-nav.component';
import { FooterComponent } from './layout/footer/footer.component';
import { HeaderComponent } from './layout/header/header.component';



@NgModule({
  declarations: [
    SideNavComponent,
    FooterComponent,
    HeaderComponent
  ],
  imports: [
    CommonModule
  ],
  exports:[
    SideNavComponent,
    FooterComponent,
    HeaderComponent
  ]
})
export class CoreModule { }
