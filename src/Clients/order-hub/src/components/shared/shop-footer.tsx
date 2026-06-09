export function ShopFooter() {
  return (
    <footer className="border-t py-6">
      <div className="mx-auto max-w-7xl px-4 text-center text-sm text-muted-foreground">
        &copy; {new Date().getFullYear()} OrderHub. All rights reserved.
      </div>
    </footer>
  );
}
