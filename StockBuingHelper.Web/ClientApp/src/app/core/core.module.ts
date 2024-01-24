import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { SideNavComponent } from './layout/side-nav/side-nav.component';
import { FooterComponent } from './layout/footer/footer.component';
import { HeaderComponent } from './layout/header/header.component';
import { HttpClientModule } from '@angular/common/http';



@NgModule({
  declarations: [
    SideNavComponent,
    FooterComponent,
    HeaderComponent
  ],
  imports: [
    CommonModule,
    HttpClientModule
  ],
  exports:[
    SideNavComponent,
    FooterComponent,
    HeaderComponent
  ]
})
export class CoreModule { }
