import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { LoginComponent } from './pages/login/components/login/login.component';
import { authGuard } from './core/guards/auth.guard';
import { VtiQueryComponent } from './pages/functions/components/vti-query/vti-query.component';

const routes: Routes = [  
  {
    path: 'login', 
    component: LoginComponent 
  },
  {
    path: 'vtiQuery', 
    canActivate : [authGuard],
    component: VtiQueryComponent 
  },
  {
    path: 'admin',
    canActivate : [authGuard],
    loadChildren: () => import('./pages/admin/admin.module').then(m => m.AdminModule)
  },
  {path: '**', redirectTo: 'vtiQuery' },//沒有比對到路由，要放到最後面
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule { }
